using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Integration.Tests
{
    public static class IntegrationTestHelper
    {
        public static void UpdateMany(params PageData[] pages)
        {
            foreach (var page in pages)
                Update(page);
        }

        public static IndexingStatus Update(IContent content)
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var indexer = new Indexer(new CoreIndexer(settings), settings, null);

            return indexer.Update(content);
        }
    }
}
