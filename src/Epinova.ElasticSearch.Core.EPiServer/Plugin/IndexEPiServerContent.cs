using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;
using EpiJobBase = EPiServer.Scheduler.ScheduledJobBase;
// ReSharper disable VirtualMemberNeverOverridden.Global

namespace Epinova.ElasticSearch.Core.EPiServer.Plugin
{
    [ScheduledPlugIn(SortIndex = 100000, DisplayName = "Elasticsearch: Index EPiServer contents",
        Description = "Indexes EPiServer content in Elasticsearch.")]
    public class IndexEPiServerContent : EpiJobBase
    {
        private readonly IContentLoader _contentLoader;
        private readonly ICoreIndexer _coreIndexer;
        private readonly IIndexer _indexer;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly ILogger _logger;
        protected readonly IElasticSearchSettings Settings;
        protected string CustomIndexName;

        public IndexEPiServerContent()
        {
            IsStoppable = true;

            _logger = LogManager.GetLogger(typeof(IndexEPiServerContent));

            _indexer = ServiceLocator.Current.GetInstance<IIndexer>();
            _coreIndexer = ServiceLocator.Current.GetInstance<ICoreIndexer>();
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            _languageBranchRepository = ServiceLocator.Current.GetInstance<ILanguageBranchRepository>();
            Settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        }

        private bool IsStopped { get; set; }

        public override void Stop()
        {
            base.Stop();
            IsStopped = true;
        }

        public override string Execute()
        {
            var stopwatch = Stopwatch.StartNew();
            var finalStatus = new StringBuilder();
            var skippedReason = new StringBuilder();
            var results = new BulkBatchResult();
            var bulkCounter = 1;

            string logging = $"Indexing starting. Content retrived and indexed in bulks of {Settings.BulkSize} items.";

            _logger.Information(logging);
            OnStatusChanged(logging);

            try
            {
                var languages = _languageBranchRepository.ListEnabled();
                var contentReferences = GetContentReferences();

                logging = $"Retrieved {contentReferences.Count} ContentReference items from the following languages: {string.Join(", ", languages.Select(l => l.LanguageID))}";
                _logger.Information(logging);
                OnStatusChanged(logging);

                //
                while (contentReferences.Any())
                {
                    var contents = GetDescendentContents(contentReferences.Take(Settings.BulkSize).ToList(), languages, bulkCounter);
                    Type[] contentTypes = contents.Select(c => c.GetOriginalType()).Distinct().ToArray();

                    UpdateMappings(languages, contentTypes);

                    var batchResult = IndexContents(contents, bulkCounter);
                    results.Batches.AddRange(batchResult.Batches);

                    contentReferences.RemoveRange(0, contentReferences.Count >= Settings.BulkSize
                            ? Settings.BulkSize
                            : contentReferences.Count);

                    bulkCounter++;
                }

                if (IsStopped)
                    return "Aborted by user";
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                finalStatus.AppendLine();
                finalStatus.AppendLine(ex.Message);
                finalStatus.AppendLine("<pre>" + ex.StackTrace + "</pre>");

                // If we re-throw here, stacktrace won't be displayed
            }

            stopwatch.Stop();

            var finished = String.Format("Indexing complete. Content retrieved and indexed in {1} bulks of {3} items. Processed {2} batches of {3} items, for a total of {4} items to Elasticsearch index. Time elapsed: {0}. ",
                bulkCounter, stopwatch.Elapsed, results.Batches.Count,
                Settings.BulkSize, results.Batches.Sum(b => b.Items.Length));

            for (var i = 1; i <= results.Batches.Count; i++)
            {
                if (results.Batches[i - 1].Errors)
                {
                    var message = " Batch " + i + " failed.";

                    foreach (var item in results.Batches[i - 1].Items.Where(item => item.Status >= 400))
                    {
                        message += item.ToString();
                    }

                    finished += message;
                    _logger.Warning(message);
                }
            }

            finalStatus.Insert(0, finished);
            OnStatusChanged(finalStatus.ToString());

            _logger.Information(skippedReason.ToString());
            _logger.Information(finished);

            finalStatus.AppendLine(skippedReason.ToString().Replace("\n", "<br/>"));

            return finalStatus.ToString();
        }

        private void UpdateMappings(IList<LanguageBranch> languages, Type[] contentTypes, string indexName = null)
        {
            // Update mappings
            foreach (var language in languages.Select(l => l.LanguageID))
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(indexName))
                        indexName = Settings.GetDefaultIndexName(language);

                    OnStatusChanged("Initializing index '" + indexName + "'");
                    _logger.Debug("Index: " + indexName);

                    var indexing = new Indexing(Settings);

                    if (!indexing.IndexExists(indexName))
                        throw new Exception("Index does not exist");

                    OnStatusChanged("Updating mapping for index " + indexName);

                    foreach (Type type in contentTypes)
                    {
                        if (Indexer.IsExludedType(type))
                        {
                            _logger.Information($"Skipping excluded type '{type.FullName}'");
                            continue;
                        }

                        _coreIndexer.UpdateMapping(type, typeof(IndexItem), indexName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning("Uh oh...", ex);
                }
            }
        }

        private BulkBatchResult IndexContents(List<IContent> contentItems, int bulkCounter)
        {
            List<IContent> contentToIndex = GetContentToIndex(contentItems, bulkCounter);

            // Perform bulk update
            OnStatusChanged($"Bulk{bulkCounter} - Preparing bulk update of {contentToIndex.Count} items...");
            return _indexer.BulkUpdate(contentToIndex, str =>
            {
                OnStatusChanged(str);
                _logger.Debug(str);
            }, CustomIndexName);
        }


        protected virtual List<ContentReference> GetContentReferences()
        {
            OnStatusChanged("Loading all references from database...");
            return _contentLoader.GetDescendents(ContentReference.RootPage).ToList();
        }

        protected virtual List<IContent> GetDescendentContents(List<ContentReference> contentReferences,
            IList<LanguageBranch> languages, int bulkCounter)
        {
            OnStatusChanged($"Bulk{bulkCounter} - Loading all contents from database...");

            var contentItems = new List<IContent>();

            foreach (var languageBranch in languages)
            {
                contentItems.AddRange(_contentLoader.GetItems(contentReferences, languageBranch.Culture));
            }

            return contentItems;
        }

        protected virtual List<IContent> GetContentToIndex(List<IContent> contentItems, int bulkCounter)
        {
            // Indexable properties (string, XhtmlString, [Searchable(true)]) 
            var allIndexableProperties = new List<KeyValuePair<string, Type>>();
            var contentToIndex = new List<IContent>();
            var counter = 0;

            foreach (IContent content in contentItems)
            {
                counter++;

                if (counter % 100 == 0)
                    OnStatusChanged($"Bulk{bulkCounter} - Analyzing content item {counter} of {contentItems.Count}...");

                if (IsStopped)
                    return contentToIndex;

                // Get indexable properties (string, XhtmlString, [Searchable(true)]) 
                var indexableProperties = content.GetType().GetIndexableProps(false)
                    .Where(p => allIndexableProperties.All(kvp => kvp.Key != p.Name))
                    .Select(p => new KeyValuePair<string, Type>(p.Name, p.PropertyType))
                    .ToList();

                allIndexableProperties.AddRange(indexableProperties);

                if (_logger.IsDebugEnabled())
                {
                    _logger.Debug("Indexable properties:");
                    indexableProperties.ForEach(p => { _logger.Debug(p.Key); });
                }

                contentToIndex.Add(content);
            }

            return contentToIndex;
        }
    }
}