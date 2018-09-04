using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using TestData;
using NUnit.Framework;

namespace Integration.Tests
{
    [TestFixture, Category("Integration")]
    public class FacetTests
    {
        private IElasticSearchService _service;

        [SetUp]
        public void Setup()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();
        }


        [Test]
        public void FacetsFor_CreatesRawMappingAndReturnsFacetsAfterReIndex()
        {
            if(Server.Info.Version.Major >= 5)
                return;

            TestPage page1 = Factory.GetPageData<TestPage>();
            TestPage page2 = Factory.GetPageData<TestPage>();
            page1.TestProp = "page 1";
            page2.TestProp = "page 2";

            IntegrationTestHelper.UpdateMany(page1, page2);

            // The initial search will trigger mapping-update (for v2.x)
            _service.Search<TestPage>("page")
                .FacetsFor(p => p.TestProp)
                .GetResults();

            IndexMapping mapping = Mapping.GetIndexMapping(typeof(IndexItem), "no", ElasticFixtureSettings.IndexName);

            // _raw mapping created
            Assert.True(mapping.Properties.ContainsKey("TestProp_raw"));

            // Copy-to created
            Assert.Contains("TestProp_raw", mapping.Properties["TestProp"].CopyTo);

            // Correct analyzer set
            Assert.AreEqual("raw", mapping.Properties["TestProp_raw"].Analyzer);


            // Now re-index the content to enable facets
            IntegrationTestHelper.UpdateMany(page1, page2);

            SearchResult result = _service.Search<TestPage>("page")
                .FacetsFor(p => p.TestProp)
                .GetResults();

            Assert.True(result.Facets.First().Hits.Any());
        }
    }
}
