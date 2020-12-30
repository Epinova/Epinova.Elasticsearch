using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly IContentIndexService _contentIndexService;
        private readonly ICoreIndexer _coreIndexer;
        private readonly IIndexer _indexer;
        private readonly IBestBetsRepository _bestBetsRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly IElasticSearchSettings _settings;
        private readonly IServerInfoService _serverInfoService;
        
        private readonly Utilities.Indexing _indexing;
        protected string CustomIndexName;

        public IndexEPiServerContent(
            IContentIndexService contentIndexService,
            IContentLoader contentLoader,
            ICoreIndexer coreIndexer,
            IIndexer indexer,
            IBestBetsRepository bestBetsRepository,
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper)
        {
            _coreIndexer = coreIndexer;
            _indexer = indexer;
            _bestBetsRepository = bestBetsRepository;
            _contentIndexService = contentIndexService;
            _languageBranchRepository = languageBranchRepository;
            _settings = settings;
            _contentLoader = contentLoader;
            _serverInfoService = serverInfoService;
            _indexing = new Utilities.Indexing(serverInfoService, settings, httpClientHelper);
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

            var failed = false;
            var finalStatus = new StringBuilder();
            var skippedReason = new StringBuilder();
            var results = new BulkBatchResult();
            var logMessage = $"Indexing starting. Content retrieved and indexed in bulks of {_settings.BulkSize} items.";

            _logger.Information(logMessage);
            OnStatusChanged(logMessage);

            try
            {
                var languages = _languageBranchRepository.ListEnabled();

                if(!IndicesExists(languages))
                {
                    throw new InvalidOperationException("One or more indices is missing, please create them.");
                }

                if(!_serverInfoService.ListPlugins().Any(p => p.Component.Equals("ingest-attachment")))
                {
                    throw new InvalidOperationException("Plugin 'ingest-attachment' is missing, please install it.");
                }

                List<ContentReference> contentReferences = GetContentReferences();

                logMessage = $"Retrieved {contentReferences.Count} ContentReference items from the following languages: {String.Join(", ", languages.Select(l => l.LanguageID))}";
                _logger.Information(logMessage);
                OnStatusChanged(logMessage);

                var contentList = new List<IContent>();

                // Fetch all content to index
                while(contentReferences.Count > 0)
                {
                    if(IsStopped)
                        return "Aborted by user";

                    List<IContent> contents = GetDescendentContents(contentReferences.Take(_settings.BulkSize).ToList(), languages);

                    contents.RemoveAll(_indexer.SkipIndexing);
                    contents.RemoveAll(_indexer.IsExcludedType);

                    contentList.AddRange(contents);
                    var removeCount = contentReferences.Count >= _settings.BulkSize ? _settings.BulkSize : contentReferences.Count;
                    contentReferences.RemoveRange(0, removeCount);
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
                finalStatus
                    .AppendLine()
                    .AppendLine(ex.Message)
                    .AppendLine()
                    .AppendLine("Stacktrace:")
                    .AppendLine(ex.StackTrace);

                failed = true;
                // If we re-throw here, stacktrace won't be displayed
            }

            var finishedBuilder = new StringBuilder($"Processed {results.Batches.Count} batches, for a total of {results.Batches.Sum(b => b?.Items?.Length ?? 0)} items to Elasticsearch index.\n");

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

            finalStatus.Insert(0, finishedBuilder.ToString());
            OnStatusChanged(finalStatus.ToString());

            _logger.Information(skippedReason.ToString());
            _logger.Information(finishedBuilder.ToString());

            finalStatus.AppendLine(skippedReason.ToString());

            var status = finalStatus.ToString().Replace("\n", "<br/>");

            if(failed)
                throw new Exception(status);

            return status;
        }

        protected virtual List<ContentReference> GetContentReferences()
        {
            OnStatusChanged("Loading all references from database...");
            return _contentLoader.GetDescendents(ContentReference.RootPage).ToList();
        }

        protected virtual List<IContent> GetDescendentContents(List<ContentReference> contentReferences, IEnumerable<LanguageBranch> languages)
        {
            return _contentIndexService.ListContent(contentReferences, languages.ToList()).ToList();
        }

        private bool IndicesExists(IEnumerable<LanguageBranch> languages)
        {
            foreach(var language in languages.Select(l => l.LanguageID))
            {
                var indexName = GetIndexName(language);
                if(!_indexing.IndexExists(indexName))
                {
                    return false;
                }
            }

            return true;
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
        
        private string GetIndexName(string language) => !String.IsNullOrEmpty(CustomIndexName) ? _settings.GetCustomIndexName(CustomIndexName, language) : _settings.GetDefaultIndexName(language);

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