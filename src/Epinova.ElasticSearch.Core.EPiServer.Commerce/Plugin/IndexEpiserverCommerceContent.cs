using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Plugin;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using Mediachase.Commerce.Catalog;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Plugin
{
    [ScheduledPlugIn(
        SortIndex = 100001,
        DisplayName = "Elasticsearch: Index Commerce content",
        Description = "Indexes Episerver Commerce content in Elasticsearch.")]
    public class IndexEpiserverCommerceContent : IndexEPiServerContent
    {
        private readonly IContentLoader _contentLoader;
        private readonly ReferenceConverter _referenceConverter;

        public IndexEpiserverCommerceContent(
            IContentLoader contentLoader,
            IContentIndexService contentIndexService,
            ICoreIndexer coreIndexer,
            IIndexer indexer,
            IBestBetsRepository bestBetsRepository,
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper,
            ReferenceConverter referenceConverter)
            : base(contentIndexService, contentLoader, coreIndexer, indexer, bestBetsRepository, languageBranchRepository, settings, serverInfoService, httpClientHelper)
        {
            _contentLoader = contentLoader;
            _referenceConverter = referenceConverter;
            CustomIndexName = $"{settings.Index}-{Constants.CommerceProviderName}";
        }

        protected override List<ContentReference> GetContentReferences()
        {
            OnStatusChanged("Loading all references from database...");
            return _contentLoader.GetDescendents(_referenceConverter.GetRootLink()).ToList();
        }
    }
}