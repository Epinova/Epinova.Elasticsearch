using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.Events;
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
        DisplayName = Constants.IndexEPiServerContentDisplayName,
        Description = "Indexes CMS content in Elasticsearch.")]
    public class IndexEPiServerContent : ScheduledJobBase
    {
        public static event OnBeforeIndexContent BeforeIndexContent;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(IndexEPiServerContent));
        private readonly IContentLoader _contentLoader;
        private readonly ICoreIndexer _coreIndexer;
        private readonly IIndexer _indexer;
        private readonly IBestBetsRepository _bestBetsRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;
        protected string CustomIndexName;

        public IndexEPiServerContent(
            IContentLoader contentLoader,
            ICoreIndexer coreIndexer,
            IIndexer indexer,
            IBestBetsRepository bestBetsRepository,
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper)
        {
            _coreIndexer = coreIndexer;
            _indexer = indexer;
            _bestBetsRepository = bestBetsRepository;
            _contentLoader = contentLoader;
            _languageBranchRepository = languageBranchRepository;
            _settings = settings;
            _httpClientHelper = httpClientHelper;
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

                    contents.RemoveAll(_indexer.SkipIndexing);
                    contents.RemoveAll(_indexer.IsExcludedType);

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

                var mediaData = contentList.OfType<MediaData>().ToList();
                contentList.RemoveAll(c => c is MediaData);

                var bulkCount = Math.Ceiling(contentList.Count / (double)_settings.BulkSize);
                for(var i = 1; i <= bulkCount; i++)
                {
                    OnStatusChanged($"Indexing bulk {i} of {bulkCount} (Bulk size: {_settings.BulkSize})");
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

                // Index media files one by one regardless of bulk size.
                var mediaBatchResults = IndexMediaData(mediaData);
                results.Batches.AddRange(mediaBatchResults.Batches);

                // Put Best Bets back into the index
                RestoreBestBets(languages);
            }
            catch(Exception ex)
            {
                _logger.Error(ex.Message, ex);
                finalStatus.AppendLine();
                finalStatus.AppendLine(ex.Message);
                finalStatus.Append("<pre>").Append(ex.StackTrace).AppendLine("</pre>");
                // If we re-throw here, stacktrace won't be displayed
            }

            var finishedBuilder = new StringBuilder($"Processed {results.Batches.Count} batches, for a total of {results.Batches.Sum(b => b?.Items?.Length ?? 0)} items to Elasticsearch index.");

            for(var i = 1; i <= results.Batches.Count; i++)
            {
                if(results.Batches[i - 1]?.Errors ?? false)
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

        private bool IndicesExists(IEnumerable<LanguageBranch> languages)
        {
            foreach(var language in languages.Select(l => l.LanguageID))
            {
                var indexName = GetIndexName(language);
                var index = new Index(_settings, _httpClientHelper, indexName);
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
                var index = new Index(_settings, _httpClientHelper, indexName);
                count += index.GetDocumentCount();
            }

            return count;
        }

        private void RestoreBestBets(IEnumerable<LanguageBranch> languages)
        {
            foreach(var language in languages.Select(l => l.LanguageID))
            {
                try
                {
                    var indexName = GetIndexName(language);
                    _logger.Debug("Index: " + indexName);
                    OnStatusChanged("Restoring best bets for index " + indexName);
                    var bestBets = _bestBetsRepository.GetBestBets(language, indexName);
                    foreach(var bestBet in bestBets)
                    {
                        _coreIndexer.UpdateBestBets(indexName, typeof(IndexItem), bestBet.Id, bestBet.GetTerms());
                    }
                }
                catch(Exception ex)
                {
                    _logger.Warning("Failed to update mappings", ex);
                }
            }
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
                        _coreIndexer.CreateAnalyzedMappingsIfNeeded(type, language, indexName);
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

        private BulkBatchResult IndexMediaData(IList<MediaData> mediaData)
        {
            var filteredMediaData = mediaData.Distinct()
                .Where(IsAllowedExtension).ToList();

            var message = $"Indexing {filteredMediaData.Count} media data";
            _logger.Information(message);
            OnStatusChanged(message);

            var mediaBatchResults = new BulkBatchResult();
            for(var i = 0; i < filteredMediaData.Count; i++)
            {
                var batchResult = IndexContents(new[] { filteredMediaData[i] });
                mediaBatchResults.Batches.AddRange(batchResult.Batches);
            }
            return mediaBatchResults;

            static bool IsAllowedExtension(MediaData m)
            {
                return Indexing.IncludedFileExtensions
                    .Contains(Path.GetExtension(m.RouteSegment ?? String.Empty).Trim(' ', '.').ToLower());
            }
        }

        private BulkBatchResult IndexContents(IEnumerable<IContent> contentItems)
        {
            // Perform bulk update
            return _indexer.BulkUpdate(contentItems, str =>
            {
                OnStatusChanged(str);
                _logger.Debug(str);
            }, CustomIndexName);
        }
    }
}