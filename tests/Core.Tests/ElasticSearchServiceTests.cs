using System;
using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using TestData;
using Xunit;

namespace Core.Tests
{
    public class ElasticSearchServiceTests
    {
        private readonly ElasticSearchService<ComplexType> _service;

        public ElasticSearchServiceTests()
        {
            Factory.SetupServiceLocator();
            _service = new ElasticSearchService<ComplexType>();
        }

        [Theory]
        [InlineData(100, true)]
        [InlineData(1, true)]
        [InlineData(0, false)]
        public void Boost_AddsFieldIfValid(byte weight, bool shouldAdd)
        {
            var result = _service.Boost(x => x.StringProperty, weight) as ElasticSearchService<ComplexType>;
            bool added = result.BoostFields.Any(x => x.Value == weight);

            Assert.Equal(added, shouldAdd);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        public void From_SetsFromValue(int from)
        {
            var result = _service.From(from) as ElasticSearchService<ComplexType>;

            Assert.Equal(result.FromValue, from);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Skip_SetsFromValue(int skip)
        {
            var result = _service.Skip(skip) as ElasticSearchService<ComplexType>;

            Assert.Equal(result.FromValue, skip);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Size_SetsSizeValue(int size)
        {
            var result = _service.Size(size) as ElasticSearchService<ComplexType>;

            Assert.Equal(result.SizeValue, size);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1000)]
        public void Take_SetsSizeValue(int take)
        {
            var result = _service.Take(take) as ElasticSearchService<ComplexType>;

            Assert.Equal(result.SizeValue, take);
        }

        [Fact]
        public void SortBy_SetsCorrectField()
        {
            var result = _service.SortBy(x => x.StringProperty) as ElasticSearchService<ComplexType>;
            const string fieldName = "StringProperty";

            Assert.Equal(result.SortFields[0].FieldName, fieldName);
        }

        [Fact]
        public void SortBy_SetsCorrectDirection()
        {
            var result = _service.SortBy(x => x.StringProperty) as ElasticSearchService<ComplexType>;

            Assert.Equal("asc", result.SortFields[0].Direction);
        }

        [Fact]
        public void SortByDescending_SetsCorrectDirection()
        {
            var result = _service.SortByDescending(x => x.StringProperty) as ElasticSearchService<ComplexType>;

            Assert.Equal("desc", result.SortFields[0].Direction);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(1337)]
        public void StartFrom_AfterSearch_SetsCorrectRootId(int root)
        {
            IElasticSearchService<ComplexType> result = _service.Search<ComplexType>("").StartFrom(root);

            Assert.Equal(root, result.RootId);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(1337)]
        public void StartFrom_BeforeSearch_SetsCorrectRootId(int root)
        {
            IElasticSearchService<ComplexType> result = _service.StartFrom(root).Search<ComplexType>("");

            Assert.Equal(root, result.RootId);
        }

        [Fact]
        public void InField_AddsField()
        {
            var result = _service.InField(x => x.StringProperty) as ElasticSearchService<ComplexType>;

            Assert.Contains("StringProperty", result.SearchFields);
        }

        [Fact]
        public void InField_DuplicateFieldThrows()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _service
                    .InField(x => x.StringProperty)
                    .InField(x => x.StringProperty);
            });
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("bar")]
        [InlineData("Lorem Ipsum Bacon")]
        public void Filter_SetsCorrectTypeForString(string value)
        {
            var service = _service.Filter(x => x.StringProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "StringProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(String).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Filter_SetsCorrectTypeForBool(bool value)
        {
            var service = _service.Filter(x => x.BoolProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "BoolProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(Boolean).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Int64.MinValue)]
        [InlineData(Int64.MaxValue)]
        public void Filter_SetsCorrectTypeForLong(long value)
        {
            var service = _service.Filter(x => x.LongProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "LongProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(Int64).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Int32.MinValue)]
        [InlineData(Int32.MaxValue)]
        public void Filter_SetsCorrectTypeForInteger(int value)
        {
            var service = _service.Filter(x => x.IntProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "IntProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(Int32).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Double.MinValue)]
        [InlineData(Double.MaxValue)]
        public void Filter_SetsCorrectTypeForDouble(double value)
        {
            var service = _service.Filter(x => x.DoubleProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "DoubleProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(Double).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Single.MinValue)]
        [InlineData(Single.MaxValue)]
        public void Filter_SetsCorrectTypeForFloat(float value)
        {
            var service = _service.Filter(x => x.FloatProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "FloatProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(Single).Name, result.Type.Name);
        }

        [Fact]
        public void Filter_SetsCorrectTypeForDateTime()
        {
            var value = new DateTime(1980, 1, 30);

            var service = _service.Filter(x => x.DateTimeProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "DateTimeProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(DateTime).Name, result.Type.Name);
        }

        [Fact]
        public void Exclude_AddsRoot()
        {
            _service.Exclude(42);
            Assert.Contains(42, _service.ExcludedRoots.Keys);
        }

        [Fact]
        public void Exclude_NotRecursive_AddsCorrectRoot()
        {
            _service.Exclude(42, false);
            Assert.False(_service.ExcludedRoots[42]);
        }
    }
}
