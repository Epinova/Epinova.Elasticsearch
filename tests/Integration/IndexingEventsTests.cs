using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Events;
using Epinova.ElasticSearch.Core.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using TestData;
using NUnit.Framework;

namespace Integration.Tests
{
    [TestFixture]
    public class IndexingEventsTests
    {
        private readonly IElasticSearchService _service;
        private readonly IIndexer _indexer;

        public IndexingEventsTests()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();
            _indexer = ServiceLocator.Current.GetInstance<IIndexer>();
        }


        [Test]
        public void DeleteFromIndex_RemovesContentFromIndex()
        {
            PageData page = Factory.GetPageData();

            // Add it to the index
            _indexer.Update(page);

            // Raise the same event Episerver does
            IndexingEvents.DeleteFromIndex(null, new ContentEventArgs(page.ContentLink, page));

            SearchResult searchResult = _service.Get<PageData>().GetResults();

            Assert.False(searchResult.Hits.Any(h => h.Id == page.ContentLink.ID));
        }


        [Test]
        public void UpdateIndex_RemovesContentFromIndexIfParentIsWastebasket()
        {
            int wasteId = Factory.GetInteger();
            ContentReference.WasteBasket = new PageReference(wasteId);

            PageData page = Factory.GetPageData<PageData>(parentId: wasteId);

            // Add it to the index
            _indexer.Update(page);

            // Raise the same event Episerver does
            IndexingEvents.UpdateIndex(null, new ContentEventArgs(page.ContentLink, page)
            {
                TargetLink = ContentReference.WasteBasket
            });

            SearchResult result = _service.Get<PageData>().GetResults();

            Assert.False(result.Hits.Any(h => h.Id == page.ContentLink.ID));
        }
    }
}
