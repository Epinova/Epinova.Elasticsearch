using System;
using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.ServiceLocation;
using Moq;
using TestData;
using NUnit.Framework;

using Epinova.ElasticSearch.Core.EPiServer.Extensions;

namespace Integration.Tests
{
    [TestFixture, Category("Integration")]
    public class IndexContentFragmentTests
    {
        private IElasticSearchService _service;
        private IIndexer _indexer;

        [SetUp]
        public void Setup()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();
            _indexer = ServiceLocator.Current.GetInstance<IIndexer>();
        }


        [Ignore("Mocking/shared state issues")]
        [Test]
        public void Update_XhtmlWithBlockIsIndexed()
        {
            const string propValue = "Child TestProp value";
            Console.WriteLine("propValue: {0}", propValue);

            var childPage = Factory.GetPageData<TestPage>();
            childPage.TestProp = propValue;

            Console.WriteLine("childPage Name: {0}, Id: {1}, TestProp: {2}", childPage.Name, childPage.ContentLink.ID, childPage.TestProp);

            var contentFragment = new TestableContentFragment(childPage);

            Console.WriteLine("contentFragment Id: {0}", contentFragment.ContentLink.ID);

            var parentPage = Factory.GetPageData<TestPageXhtmlString>();
            parentPage.XhtmlString = Factory.GetXhtmlString(Factory.GetString(), contentFragment);

            var contentLoaderMock = new Mock<IContentLoader>();
            // ReSharper disable once NotAccessedVariable
            IContent content = contentFragment.GetContent();
            contentLoaderMock
                .Setup(m => m.TryGet(contentFragment.ContentLink, out content))
                .Returns(true);

            IntegrationFixture.ServiceLocationMock.ServiceLocatorMock
                .Setup(m => m.GetInstance<IContentLoader>())
                .Returns(contentLoaderMock.Object);

            _indexer.Update(parentPage);

            Console.WriteLine("Indexing parentPage. Name: {0}, Id: {1}, Xhtml: {2}", parentPage.Name, parentPage.ContentLink.ID, parentPage.XhtmlString);
            Console.WriteLine("parentPage.XhtmlString fragments:");
            foreach (ContentFragment fragment in parentPage.XhtmlString.GetFragments())
            {
                Console.WriteLine(fragment.ContentLink.ID.ToString());
            }

            List<SearchHit> searchHits = _service.Search<TestPageXhtmlString>(propValue)
                .GetResults()
                .Hits
                .ToList();

            Console.WriteLine("Hits: {0}", searchHits.Count);
            foreach (SearchHit hit in searchHits)
            {
                Console.WriteLine("Id: {0}, Name: {1}", hit.Id, hit.Name);
            }

            SearchHit searchHit = searchHits
                .FirstOrDefault(h => h.Id == parentPage.ContentLink.ID);

            Assert.NotNull(searchHit);
        }
    }
}
