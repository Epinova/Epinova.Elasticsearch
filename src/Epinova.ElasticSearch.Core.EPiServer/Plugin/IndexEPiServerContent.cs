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
using EPiServer.Scheduler;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;

namespace Epinova.ElasticSearch.Core.EPiServer.Plugin
{
    [ScheduledPlugIn(
        SortIndex = 100000,
        DisplayName = "Elasticsearch: Index CMS content",
        Description = "Indexes CMS content in Elasticsearch.")]
    public class IndexEPiServerContent : ScheduledJobBase
    {
        private readonly ILogger _logger = LogManager.GetLogger(typeof(IndexEPiServerContent));
        private readonly IContentLoader _contentLoader;
        private readonly ICoreIndexer _coreIndexer;
        private readonly IIndexer _indexer;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IElasticSearchSettings _settings;
        protected string CustomIndexName;

        public IndexEPiServerContent(
            IContentLoader contentLoader,
            ICoreIndexer coreIndexer,
            IIndexer indexer,
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings)
        {
            _indexer = indexer;
            _coreIndexer = coreIndexer;
            _contentLoader = contentLoader;
            _languageBranchRepository = languageBranchRepository;
            _settings = settings;
            IsStoppable = true;
        }

        private bool IsStopped { get; set; }

        public override void Stop()
        {
            base.Stop();
            IsStopped = true;
        }

        public override string Execute()
        {
            var finalStatus = new StringBuilder();
            var skippedReason = new StringBuilder();
            var results = new BulkBatchResult();
            var bulkCounter = 1;
            var logMessage = $"Indexing starting. Content retrived and indexed in bulks of {_settings.BulkSize} items.";

            _logger.Information(logMessage);
            OnStatusChanged(logMessage);

            try
            {
                var languages = _languageBranchRepository.ListEnabled();
                var contentReferences = GetContentReferences();

                logMessage = $"Retrieved {contentReferences.Count} ContentReference items from the following languages: {String.Join(", ", languages.Select(l => l.LanguageID))}";
                _logger.Information(logMessage);
                OnStatusChanged(logMessage);

                while (contentReferences.Count > 0)
                {
                    var contents = GetDescendentContents(contentReferences.Take(_settings.BulkSize).ToList(), languages, bulkCounter);
                    var batchResult = IndexContents(contents, bulkCounter);
                    results.Batches.AddRange(batchResult.Batches);

                    contentReferences.RemoveRange(0, contentReferences.Count >= _settings.BulkSize
                            ? _settings.BulkSize
                            : contentReferences.Count);

                    bulkCounter++;
                }

                UpdateMappings(languages, CustomIndexName);

                if (IsStopped)
                    return "Aborted by user";
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                finalStatus.AppendLine();
                finalStatus.AppendLine(ex.Message);
                finalStatus.Append("<pre>").Append(ex.StackTrace).AppendLine("</pre>");
                // If we re-throw here, stacktrace won't be displayed
            }

            var finished = $"Processed {results.Batches.Count} batches of {_settings.BulkSize} items, for a total of {results.Batches.Sum(b => b.Items.Length)} items to Elasticsearch index.";

            for (var i = 1; i <= results.Batches.Count; i++)
            {
                if (results.Batches[i - 1].Errors)
                {
                    var message = $" Batch {i} failed.";

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

        private void UpdateMappings(IList<LanguageBranch> languages, string indexName = null)
        {
            // Update mappings
            foreach (var language in languages.Select(l => l.LanguageID))
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(indexName))
                        indexName = _settings.GetDefaultIndexName(language);
                    else
                        indexName = _settings.GetCustomIndexName(indexName, language);

                    _logger.Debug("Index: " + indexName);

                    var indexing = new Indexing(_settings);

                    if (!indexing.IndexExists(indexName))
                        throw new Exception("Index does not exist");

                    OnStatusChanged("Updating mapping for index " + indexName);

                    _coreIndexer.UpdateMapping(typeof(IndexItem), typeof(IndexItem), indexName);

                    ContentExtensions.CreateAnalyzedMappingsIfNeeded(typeof(IndexItem), language, indexName);
                    ContentExtensions.CreateDidYouMeanMappingsIfNeeded(typeof(IndexItem), language, indexName);
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
            OnStatusChanged($"Bulk {bulkCounter} - Preparing bulk update of {contentToIndex.Count} items...");
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
            OnStatusChanged($"Bulk {bulkCounter} - Loading all contents from database...");

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
                    OnStatusChanged($"Bulk {bulkCounter} - Analyzing content item {counter} of {contentItems.Count}...");

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
                    indexableProperties.ForEach(p => _logger.Debug(p.Key));
                }

                contentToIndex.Add(content);
            }

            return contentToIndex;
        }
    }
}