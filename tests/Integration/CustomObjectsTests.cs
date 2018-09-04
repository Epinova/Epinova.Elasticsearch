using System;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.ServiceLocation;
using TestData;
using NUnit.Framework;

namespace Integration.Tests
{
    [TestFixture, Category("Integration")]
    public class CustomObjectsTests
    {
        private IElasticSearchService _service;
        private IElasticSearchSettings _settings;

        [SetUp]
        public void Setup()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();
            _settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        }


        [Test]
        public void CustomObjectsAreIndexedAndSearchable()
        {
            var obj1 = new ComplexType { StringProperty = "this is myobj 1" };
            var obj2 = new ComplexType { StringProperty = "this is myobj 2" };
            var obj3 = new ComplexType { StringProperty = "this is myobj 3" };

            ICoreIndexer coreIndexer = new CoreIndexer(_settings);
            coreIndexer.Bulk(new[]
            {
                new BulkOperation(obj1, "no"),
                new BulkOperation(obj2, "no"),
                new BulkOperation(obj3, "no")
            }, Console.WriteLine);

            IElasticSearchService<ComplexType> query = _service
                .UseIndex(ElasticFixtureSettings.IndexName)
                .Language(CultureInfo.CreateSpecificCulture("no"))
                .Search<ComplexType>("myobj")
                .InField(x => x.StringProperty);

            CustomSearchResult<ComplexType> results = query.GetCustomResults();

            Assert.True(results.Hits.Any(h => h.Item.Id == obj1.Id));
            Assert.True(results.Hits.Any(h => h.Item.Id == obj2.Id));
            Assert.True(results.Hits.Any(h => h.Item.Id == obj3.Id));
        }

        [Test]
        public void CustomObjectsHasHighlight()
        {
            Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
                .ForType<ComplexType>()
                .EnableHighlighting(p => p.StringProperty);

            var obj1 = new ComplexType { StringProperty = "this is myobj 1" };

            ICoreIndexer coreIndexer = new CoreIndexer(_settings);
            coreIndexer.Bulk(new[]
            {
                new BulkOperation(obj1, "no")
            }, Console.WriteLine);

            IElasticSearchService<ComplexType> query = _service
                .UseIndex(ElasticFixtureSettings.IndexName)
                .Language(CultureInfo.CreateSpecificCulture("no"))
                .Search<ComplexType>("myobj")
                .Highlight()
                .InField(x => x.StringProperty);

            CustomSearchResult<ComplexType> results = query.GetCustomResults();

            StringAssert.Contains("<mark>myobj</mark>", results.Hits.First().Highlight);
        }

        [Test]
        public void CustomObjectsIndexesNumericTypes()
        {
            var obj1 = new ComplexType
            {
                StringProperty = "this is myobj 1",
                DecimalProperty = 42.1m,
                DoubleProperty = 42.1d,
                FloatProperty = 42.1f,
                LongProperty = 42,
                IntProperty = 42
            };

            ICoreIndexer coreIndexer = new CoreIndexer(_settings);
            coreIndexer.Bulk(new[]
            {
                new BulkOperation(obj1, "no")
            }, Console.WriteLine);

            IElasticSearchService<ComplexType> query = _service
                .UseIndex(ElasticFixtureSettings.IndexName)
                .Language(CultureInfo.CreateSpecificCulture("no"))
                .Search<ComplexType>("myobj")
                .InField(x => x.StringProperty);

            CustomSearchResult<ComplexType> results = query.GetCustomResults();

            var hitItem = results.Hits.First().Item;

            Assert.AreEqual(42.1m, hitItem.DecimalProperty);
            Assert.AreEqual(42.1d, hitItem.DoubleProperty);
            Assert.AreEqual(42.1f, hitItem.FloatProperty);
            Assert.AreEqual(42, hitItem.LongProperty);
            Assert.AreEqual(42, hitItem.IntProperty);
        }
    }
}
