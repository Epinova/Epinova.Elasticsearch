using System;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using EPiServer.ServiceLocation;
using TestData;
using NUnit.Framework;

namespace Integration.Tests
{
    [TestFixture]
    public class HighlightingTests : IDisposable
    {
        private IElasticSearchService _service;

        [SetUp]
        public void Setup()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();

            Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
                .ForType<TestPage>()
                .EnableHighlighting(p => p.TestProp);
        }

        public void Dispose()
        {
            Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
                .SetHighlightTag("mark");
            Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
                .SetHighlightFragmentSize(150);
        }


        [Test]
        public void Highlight_ContainsCorrectMarkup()
        {
            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            page1.TestProp = "this is page 1";
            page2.TestProp = "this is page 2";

            IntegrationTestHelper.UpdateMany(page1, page2);

            SearchResult searchResult = _service.Search<TestPage>("page")
                .Highlight()
                .GetResults();

            string result = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID).Highlight;
            StringAssert.Contains("<mark>page</mark>", result);
        }


        [Test]
        public void Highlight_ContainsCorrectMarkupWhenCustomized()
        {
            Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
                .SetHighlightTag("strong");

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            page1.TestProp = "this is page 1";
            page2.TestProp = "this is page 2";

            IntegrationTestHelper.UpdateMany(page1, page2);

            SearchResult searchResult = _service.Search<TestPage>("page")
                .Highlight()
                .GetResults();

            string result = searchResult.Hits.First(x => x.Id == page1.ContentLink.ID).Highlight;

            StringAssert.Contains("<strong>page</strong>", result);
        }
    }
}
