using System;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using TestData;
using NUnit.Framework;
using System.Threading;

namespace Integration.Tests
{
    [TestFixture, Category("Integration")]
    public class CustomIndexTests
    {
        private readonly string _customIndexPrefix = Guid.NewGuid().ToString();
        private IElasticSearchService _service;
        private IElasticSearchSettings _settings;
        private ICoreIndexer _indexer;
        private Indexing _indexing;

        private readonly ComplexType _obj1 = new ComplexType { StringProperty = "this is myobj 1" };
        private readonly ComplexType _obj2 = new ComplexType { StringProperty = "this is myobj 2" };
        private readonly ComplexType _obj3 = new ComplexType { StringProperty = "this is myobj 3" };

        [SetUp]
        public void Setup()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();
            _settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            _indexer = new CoreIndexer(_settings);
            _indexing = new Indexing(_settings);
        }

        [TearDown]
        public void Cleanup()
        {
            _indexing.DeleteIndex($"{_customIndexPrefix}*");
        }


        [Test]
        public void CustomObjectsAreIndexedAndSearchable()
        {
            string customIndex = $"{_customIndexPrefix}-no";

            CreateCustomIndex(customIndex, typeof(ComplexType));

            ICoreIndexer coreIndexer = new CoreIndexer(_settings);
            coreIndexer.Bulk(new[]
            {
                new BulkOperation(_obj1, "no", index: customIndex),
                new BulkOperation(_obj2, "no", index: customIndex), 
                new BulkOperation(_obj3, "no", index: customIndex)
            }, Console.WriteLine);

            CustomSearchResult<ComplexType> results = Search(customIndex);

            Assert.True(results.Hits.Any(h => h.Item.Id == _obj1.Id));
            Assert.True(results.Hits.Any(h => h.Item.Id == _obj2.Id));
            Assert.True(results.Hits.Any(h => h.Item.Id == _obj3.Id));
        }

        [Test]
        public void CustomObjectsAreIndexedAndSearchableInDifferentIndexes()
        {
            string customIndex1 = $"{_customIndexPrefix}-1-no";
            string customIndex2 = $"{_customIndexPrefix}-2-no";
            string customIndex3 = $"{_customIndexPrefix}-3-no";

            CreateCustomIndex(customIndex1, typeof(ComplexType));
            CreateCustomIndex(customIndex2, typeof(ComplexType));
            CreateCustomIndex(customIndex3, typeof(ComplexType));

            Thread.Sleep(5000);

            ICoreIndexer coreIndexer = new CoreIndexer(_settings);
            coreIndexer.Bulk(new[]
            {
                new BulkOperation(_obj1, "no", index: customIndex1),
                new BulkOperation(_obj2, "no", index: customIndex2),
                new BulkOperation(_obj3, "no", index: customIndex3)
            }, Console.WriteLine);

            CustomSearchResult<ComplexType> result1 = Search(customIndex1);
            CustomSearchResult<ComplexType> result2 = Search(customIndex2);
            CustomSearchResult<ComplexType> result3 = Search(customIndex3);

            Assert.True(result1.Hits.Any(h => h.Item.Id == _obj1.Id));
            Assert.True(result2.Hits.Any(h => h.Item.Id == _obj2.Id));
            Assert.True(result3.Hits.Any(h => h.Item.Id == _obj3.Id));
        }

        private CustomSearchResult<ComplexType> Search(string index)
        {
            IElasticSearchService<ComplexType> query = _service
                .UseIndex(index)
                .Language(CultureInfo.CreateSpecificCulture("no"))
                .Search<ComplexType>("myobj")
                .InField(x => x.StringProperty);

            return query.GetCustomResults();
        }

        private void CreateCustomIndex(string index, Type type)
        {
            var indexHelper = new Index(_settings, index);
            indexHelper.Initialize(type ?? ElasticFixtureSettings.IndexType);

            indexHelper.DisableDynamicMapping(type);
            _indexer.UpdateMapping(type, type, index, "no", true);
        }
    }
}
