using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Events;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Logging;
using EPiServer.PlugIn;
using EPiServer.Scheduler;

namespace Epinova.ElasticSearch.Core.EPiServer.Plugin
{
    [ScheduledPlugIn(
        SortIndex = 100000,
        DisplayName = "Elasticsearch: Index CMS content",
        Description = "Indexes CMS content in Elasticsearch.")]
    public class IndexEPiServerContent : ScheduledJobBase
    {
        public static event OnBeforeIndexContent BeforeIndexContent;
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
            if(BeforeIndexContent != null)
            {
                _logger.Debug("Firing subscribed event BeforeIndexContent");
                BeforeIndexContent(EventArgs.Empty);
            }

            var finalStatus = new StringBuilder();
            var skippedReason = new StringBuilder();
            var results = new BulkBatchResult();
            var logMessage = $"Indexing starting. Content retrived and indexed in bulks of {_settings.BulkSize} items.";

            _logger.Information(logMessage);
            OnStatusChanged(logMessage);

            try
            {
                var languages = _languageBranchRepository.ListEnabled();

                if(!IndicesExists(languages))
                {
                    throw new InvalidOperationException("One or more indices is missing, please create them.");
                }

                if(!Server.Plugins.Any(p => p.Component.Equals("ingest-attachment")))
                {
                    throw new InvalidOperationException("Plugin 'ingest-attachment' is missing, please install it.");
                }

                var contentReferences = GetContentReferences();

                logMessage = $"Retrieved {contentReferences.Count} ContentReference items from the following languages: {String.Join(", ", languages.Select(l => l.LanguageID))}";
                _logger.Information(logMessage);
                OnStatusChanged(logMessage);

                var contentList = new List<IContent>();

                // Fetch all content to index
                while(contentReferences.Count > 0)
                {
                    if(IsStopped)
                    {
                        return "Aborted by user";
                    }

                    var contents = GetDescendentContents(contentReferences.Take(_settings.BulkSize).ToList(), languages);

                    contents.RemoveAll(Indexer.ShouldHideFromSearch);
                    contents.RemoveAll(Indexer.IsExludedType);

                    contentList.AddRange(contents);
                    var removeCount = contentReferences.Count >= _settings.BulkSize ? _settings.BulkSize : contentReferences.Count;
                    contentReferences.RemoveRange(0, removeCount);
                }

                // Is this the first run?
                var isFirstRun = GetTotalDocumentCount(languages) == 0;

                // Update mappings on first run after analyzing the actual content
                if(isFirstRun)
                {
                    var uniqueTypes = contentList.Select(content =>
                        {
                            var type = content.GetType();
                            return type.Name.EndsWith("Proxy") ? type.BaseType : type;
                        })
                        .Distinct()
                        .ToArray();

                    UpdateMappings(languages, uniqueTypes);
                }

                var bulkCount = Math.Ceiling(contentList.Count / (double)_settings.BulkSize);
                for(var i = 1; i <= bulkCount; i++)
                {
                    OnStatusChanged($"Bulk {i} of {bulkCount} - Preparing bulk update of {_settings.BulkSize} items...");
                    var batch = contentList.Take(_settings.BulkSize);
                    var batchResult = IndexContents(batch);
                    results.Batches.AddRange(batchResult.Batches);
                    var removeCount = contentList.Count >= _settings.BulkSize ? _settings.BulkSize : contentList.Count;
                    contentList.RemoveRange(0, removeCount);

                    if(IsStopped)
                    {
                        return "Aborted by user";
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex.Message, ex);
                finalStatus.AppendLine();
                finalStatus.AppendLine(ex.Message);
                finalStatus.Append("<pre>").Append(ex.StackTrace).AppendLine("</pre>");
                // If we re-throw here, stacktrace won't be displayed
            }

            var finishedBuilder = new StringBuilder($"Processed {results.Batches.Count} batches of {_settings.BulkSize} items, for a total of {results.Batches.Sum(b => b.Items.Length)} items to Elasticsearch index.");

            for(var i = 1; i <= results.Batches.Count; i++)
            {
                if(results.Batches[i - 1].Errors)
                {
                    var messageBuilder = new StringBuilder($" Batch {i} failed. Details: \n");

                    foreach(var item in results.Batches[i - 1].Items.Where(item => item.Status >= 400))
                    {
                        messageBuilder.AppendLine(item.ToString());
                    }

                    var message = messageBuilder.ToString();
                    finishedBuilder.AppendLine(message);
                    _logger.Warning(message);
                }
            }

            finalStatus.Insert(0, finishedBuilder.ToString().Replace("\n", "<br/>"));
            OnStatusChanged(finalStatus.ToString());

            _logger.Information(skippedReason.ToString());
            _logger.Information(finishedBuilder.ToString());

            finalStatus.AppendLine(skippedReason.ToString().Replace("\n", "<br/>"));

            return finalStatus.ToString();
        }

        protected virtual List<ContentReference> GetContentReferences()
        {
            OnStatusChanged("Loading all references from database...");
            return _contentLoader.GetDescendents(ContentReference.RootPage).ToList();
        }

        protected virtual List<IContent> GetDescendentContents(List<ContentReference> contentReferences, IEnumerable<LanguageBranch> languages)
        {
            var contentItems = new List<IContent>();

            foreach(var languageBranch in languages)
            {
                contentItems.AddRange(_contentLoader.GetItems(contentReferences, languageBranch.Culture));
            }

            return contentItems;
        }

        //TODO: Review the need for this
        protected virtual List<IContent> GetContentToIndex(IEnumerable<IContent> contentItems)
        {
            // Indexable properties (string, XhtmlString, [Searchable(true)]) 
            var allIndexableProperties = new List<KeyValuePair<string, Type>>();
            var contentToIndex = new List<IContent>();

            foreach(var content in contentItems)
            {
                if(IsStopped)
                {
                    break;
                }

                // Get indexable properties (string, XhtmlString, [Searchable(true)]) 
                var indexableProperties = content.GetType().GetIndexableProps(false)
                    .Where(p => allIndexableProperties.All(kvp => kvp.Key != p.Name))
                    .Select(p => new KeyValuePair<string, Type>(p.Name, p.PropertyType))
                    .ToList();

                allIndexableProperties.AddRange(indexableProperties);

                if(_logger.IsDebugEnabled())
                {
                    _logger.Debug("Indexable properties:");
                    indexableProperties.ForEach(p => _logger.Debug(p.Key));
                }

                contentToIndex.Add(content);
            }

            return contentToIndex;
        }

        private bool IndicesExists(IEnumerable<LanguageBranch> languages)
        {
            foreach(var language in languages.Select(l => l.LanguageID))
            {
                var indexName = GetIndexName(language);
                var index = new Index(_settings, indexName);
                if(!index.Exists)
                {
                    return false;
                }
            }

            return true;
        }

        private int GetTotalDocumentCount(IEnumerable<LanguageBranch> languages)
        {
            var count = 0;
            foreach(var language in languages.Select(l => l.LanguageID))
            {
                var indexName = GetIndexName(language);
                var index = new Index(_settings, indexName);
                count += index.GetDocumentCount();
            }

            return count;
        }

        private void UpdateMappings(IEnumerable<LanguageBranch> languages, Type[] contentTypes)
        {
            foreach(var language in languages.Select(l => l.LanguageID))
            {
                try
                {
                    var indexName = GetIndexName(language);
                    _logger.Debug("Index: " + indexName);
                    OnStatusChanged("Updating mapping for index " + indexName);

                    foreach(var type in contentTypes)
                    {
                        _coreIndexer.UpdateMapping(type, typeof(IndexItem), indexName);
                        ContentExtensions.CreateAnalyzedMappingsIfNeeded(type, language, indexName);
                        ContentExtensions.CreateDidYouMeanMappingsIfNeeded(type, language, indexName);
                    }
                }
                catch(Exception ex)
                {
                    _logger.Warning("Failed to update mappings", ex);
                }
            }
        }

        private string GetIndexName(string language)
        {
            if(!String.IsNullOrEmpty(CustomIndexName))
            {
                return _settings.GetCustomIndexName(CustomIndexName, language);
            }

            return _settings.GetDefaultIndexName(language);
        }

        private BulkBatchResult IndexContents(IEnumerable<IContent> contentItems)
        {
            List<IContent> contentToIndex = GetContentToIndex(contentItems);

            // Perform bulk update
            return _indexer.BulkUpdate(contentToIndex, str =>
            {
                OnStatusChanged(str);
                _logger.Debug(str);
            }, CustomIndexName);
        }
    }
}