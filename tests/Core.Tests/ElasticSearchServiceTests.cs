using System;
using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;
using TestData;
using Xunit;

namespace Core.Tests
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class ElasticSearchServiceTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ElasticSearchService<ComplexType> _service;

        public ElasticSearchServiceTests(ServiceLocatorFixture fixture)
        {
            _service = new ElasticSearchService<ComplexType>(
                fixture.ServiceLocationMock.ServerInfoMock.Object,
                fixture.ServiceLocationMock.SettingsMock.Object,
                fixture.ServiceLocationMock.HttpClientMock.Object);
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

            Assert.Equal(result.SortFields[0].FieldName, nameof(ComplexType.StringProperty));
        }

        [Fact]
        public void SortBy_GeoPoint_AddsCorrectSort()
        {
            _service.SortBy(x => x.GeoPointProperty);

            Assert.True(_service.SortFields.OfType<GeoSort>().Any());
        }

        [Fact]
        public void SortByDescending_GeoPoint_AddsCorrectSort()
        {
            _service.SortByDescending(x => x.GeoPointProperty);

            Assert.True(_service.SortFields.OfType<GeoSort>().Any(x => x.Direction == "desc"));
        }

        [Fact]
        public void SortBy_CalledTwice_ThrowsUp()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                _service.SortBy(x => x.StringProperty);
                _service.SortBy(x => x.IntProperty);
            });
        }

        [Fact]
        public void SortByScript_AddsCorrectSort()
        {
            _service.SortByScript("1", default, "number");
            Assert.True(_service.SortFields.OfType<ScriptSort>().Any());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData("123")]
        public void SortByScript_WrongType_ThrowsUp(string type)
        {
            Assert.Throws<InvalidOperationException>(() => _service.SortByScript("{}", default, type));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SortByScript_EmptyScript_ThrowsUp(string script)
        {
            Assert.Throws<InvalidOperationException>(() => _service.SortByScript(script, default, "string"));
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
            Assert.Equal(typeof(string).Name, result.Type.Name);
        }

        [Fact]
        public void Filter_MultipleFilters_SetsAll()
        {
            var service = _service
                .Filter(x => x.StringProperty, "foo", true, Operator.Or)
                .Filter(x => x.BoolProperty, true, true, Operator.Or)
                    as ElasticSearchService<ComplexType>;

            Filter result1 = service.PostFilters.Single(f => f.FieldName == "StringProperty");
            Filter result2 = service.PostFilters.Single(f => f.FieldName == "BoolProperty");

            Assert.Equal("foo", result1.Value);
            Assert.Equal(Operator.Or, result1.Operator);

            Assert.Equal(true, result2.Value);
            Assert.Equal(Operator.Or, result2.Operator);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Filter_SetsCorrectTypeForBool(bool value)
        {
            var service = _service.Filter(x => x.BoolProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "BoolProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(bool).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Int64.MinValue)]
        [InlineData(Int64.MaxValue)]
        public void Filter_SetsCorrectTypeForLong(long value)
        {
            var service = _service.Filter(x => x.LongProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "LongProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(long).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Int32.MinValue)]
        [InlineData(Int32.MaxValue)]
        public void Filter_SetsCorrectTypeForInteger(int value)
        {
            var service = _service.Filter(x => x.IntProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "IntProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(int).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Double.MinValue)]
        [InlineData(Double.MaxValue)]
        public void Filter_SetsCorrectTypeForDouble(double value)
        {
            var service = _service.Filter(x => x.DoubleProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "DoubleProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(double).Name, result.Type.Name);
        }

        [Theory]
        [InlineData(Single.MinValue)]
        [InlineData(Single.MaxValue)]
        public void Filter_SetsCorrectTypeForFloat(float value)
        {
            var service = _service.Filter(x => x.FloatProperty, value) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == "FloatProperty");

            Assert.Equal(value, result.Value);
            Assert.Equal(typeof(float).Name, result.Type.Name);
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
        public void FilterGeoBoundingBox_SetsCorrectTypeAndCoords()
        {
            var topLeft = (59.9277542, 10.7190847);
            var bottomRight = (59.8881646, 10.7983952);

            var service = _service.FilterGeoBoundingBox(x => x.GeoPointProperty, topLeft, bottomRight) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == nameof(ComplexType.GeoPointProperty));
            var value = result.Value as GeoBoundingBox;

            Assert.True(result.Type == typeof(GeoPoint));
            Assert.Equal(topLeft.Item1, value.Box.TopLeft.Lat);
            Assert.Equal(topLeft.Item2, value.Box.TopLeft.Lon);
            Assert.Equal(bottomRight.Item1, value.Box.BottomRight.Lat);
            Assert.Equal(bottomRight.Item2, value.Box.BottomRight.Lon);
        }

        [Fact]
        public void FilterGeoDistance_SetsCorrectTypeAndCoords()
        {
            var center = (59.9277542, 10.7190847);

            var service = _service.FilterGeoDistance(x => x.GeoPointProperty, "123km", center) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == nameof(ComplexType.GeoPointProperty));
            var value = result.Value as GeoDistance;

            Assert.True(result.Type == typeof(GeoPoint));
            Assert.Equal("123km", value.Distance);
            Assert.Equal(center.Item1, value.Point.Lat);
            Assert.Equal(center.Item2, value.Point.Lon);
        }

        [Fact]
        public void FilterGeoPolygon_SetsCorrectTypeAndCoords()
        {
            var polygons = new[]
            {
                (59.9702837, 10.6149134),
                (59.9459601, 11.0231964),
                (59.7789455, 10.604809)
            };

            var service = _service.FilterGeoPolygon(x => x.GeoPointProperty, polygons) as ElasticSearchService<ComplexType>;
            Filter result = service.PostFilters.Single(f => f.FieldName == nameof(ComplexType.GeoPointProperty));
            var value = result.Value as GeoPolygon;

            Assert.True(result.Type == typeof(GeoPoint));
            Assert.Equal(polygons[0].Item1, value.Points.First().Lat);
            Assert.Equal(polygons[0].Item2, value.Points.First().Lon);
            Assert.Equal(polygons[2].Item1, value.Points.Last().Lat);
            Assert.Equal(polygons[2].Item2, value.Points.Last().Lon);
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

        [Fact]
        public void Get_SetsFlag()
        {
            var result = _service.Get<TestPage>();

            Assert.True(result.IsGetQuery);
        }
    }
}
