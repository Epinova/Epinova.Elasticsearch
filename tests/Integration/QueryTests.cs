using System;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using TestData;
using static TestData.Factory;
using NUnit.Framework;
using Constants = Epinova.ElasticSearch.Core.Models.Constants;
using Epinova.ElasticSearch.Core.Settings;

namespace Integration.Tests
{
    [TestFixture]
    public class QueryTests
    {
        private IElasticSearchService _service;

        [SetUp]
        public void Setup()
        {
            _service = ServiceLocator.Current.GetInstance<IElasticSearchService>();

            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var indexing = new Indexing(settings);
            var index = new Index(settings, ElasticFixtureSettings.IndexName);

            if (!indexing.IndexExists(ElasticFixtureSettings.IndexName))
            {
                index.Initialize(ElasticFixtureSettings.IndexType);
            }

            index.WaitForStatus(20);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void StartFrom_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            SearchResult result = _service.Search<TestPage>(GetString())
                .StartFrom(42)
                .GetResults();

            StringAssert.Contains("\"Path\":", result.Query);
            StringAssert.DoesNotContain("\"Path.keyword\":", result.Query);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void Boost_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Boost(x => x.TestProp, 42)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"match\": {" +
                "\"TestProp\": {" +
                "\"query\": \"" + searchText + "\"," +
                "\"boost\": 42," +
                "\"lenient\": true," +
                "\"operator\": \"or\"" +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void Exclude_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Exclude<TestClassA>()
                .GetResults();

            string expected = RemoveWhitespace(
                "\"must_not\": [{" +
                "\"match\": {" +
                "\"Types\": \"" + typeof(TestClassA).GetTypeName().ToLower() + "\"" +
                "}}]");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }



        
        [TestCase(2)]
        [TestCase(5)]
        public void FacetsFor_TextProperty_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);
            string suffix = esVersion >= 5
                ? Constants.KeywordSuffix
                : Constants.RawSuffix;

            SearchResult result = _service.Search<TestPage>(searchText)
                .FacetsFor(x => x.TestProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"aggregations\": {" +
                "\"TestProp\": {" +
                "\"terms\": {" +
                "\"field\": \"TestProp" + suffix + "\"," +
                "\"size\": 1000" +
                "}}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void FacetsFor_IntProperty_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .FacetsFor(x => x.TestIntProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"aggregations\": {" +
                "\"TestIntProp\": {" +
                "\"terms\": {" +
                "\"field\": \"TestIntProp\"," +
                "\"size\": 1000" +
                "}}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2, 42)]
        [TestCase(2, null)]
        [TestCase(5, 42)]
        [TestCase(5, null)]
        public void FacetsFor_NullableIntProperty_ReturnsExpectedJson(int esVersion, int? value)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .FacetsFor(x => x.TestIntNullableProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"aggregations\": {" +
                "\"TestIntNullableProp\": {" +
                "\"terms\": {" +
                "\"field\": \"TestIntNullableProp\"," +
                "\"size\": 1000" +
                "}}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void FacetsFor_ExtensionMethod_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .FacetsFor(x => x.Prize())
                .GetResults();

            string expected = RemoveWhitespace(
                "\"aggregations\": {" +
                "\"Prize\": {" +
                "\"terms\": {" +
                "\"field\": \"Prize\"," +
                "\"size\": 1000" +
                "}}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }

        
        
        [TestCase(2)]
        [TestCase(5)]
        public void FacetsFor_Array_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .FacetsFor(x => x.Path)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"aggregations\": {" +
                "\"Path\": {" +
                "\"terms\": {" +
                "\"field\": \"Path\"," +
                "\"size\": 1000" +
                "}}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [TestCase(2)]
        [TestCase(5)]
        public void FacetsForNonString_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .FacetsFor(x => x.TestIntProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"aggregations\": {" +
                "\"TestIntProp\": {" +
                "\"terms\": {" +
                "\"field\": \"TestIntProp\"," +
                "\"size\": 1000" +
                "}}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void FacetsForNonPostFilter_ReturnsExpectedJson()
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .FacetsFor(x => x.TestIntProp, false)
                .Filter(x => x.TestIntProp, 42)
                .GetResults();

            string expected = RemoveWhitespace(@"
                ""filter"": [{
                ""term"": {
                ""TestIntProp"": 42
                }}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
            StringAssert.DoesNotContain("post_filter", actual);
        }


        [TestCase(2)]
        [TestCase(5)]
        public void Filter_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);
            string suffix = esVersion >= 5
                ? Constants.KeywordSuffix
                : Constants.RawSuffix;

            SearchResult result = _service.Search<TestPage>(searchText)
                .Filter(x => x.TestProp, "foo")
                .GetResults();

            string expected = RemoveWhitespace(
                "\"post_filter\": {" +
                "\"bool\": {" +
                "\"must\": [{" +
                "\"term\": {" +
                "\"TestProp" + suffix + "\": \"foo\"" +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [TestCase(2)]
        [TestCase(5)]
        public void FilterGroup_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);
            string suffix = esVersion >= 5
                ? Constants.KeywordSuffix
                : Constants.RawSuffix;

            string suffixNonString = esVersion >= 5
                ? null
                : Constants.RawSuffix;

            SearchResult result = _service.Search<TestPage>(searchText)
                .FilterGroup(group => group
                    .Or(page => page.TestProp, new[] { "foo", "bar" })
                    .Or(page => page.TestIntProp, new[] { 1, 2 })
                    .And(page => page.Name, "baz")
                ).GetResults();

            string expected = RemoveWhitespace(
                "\"post_filter\":{" +
                "\"bool\":{" +
                "\"must\":[{" +
                "\"bool\":{" +
                "\"should\":[{\"term\":{\"TestProp" + suffix + "\":\"foo\"}}, {\"term\":{\"TestProp" + suffix +
                "\":\"bar\"}}]," +
                "\"minimum_number_should_match\":1" +
                "}}, {" +
                "\"bool\":{" +
                "\"should\":[{\"term\":{\"TestIntProp" 
                + suffixNonString + "\":1}}, {\"term\":{\"TestIntProp" + suffixNonString +
                "\":2}}]," +
                "\"minimum_number_should_match\":1" +
                "}},{" +
                "\"bool\":{" +
                "\"must\":[{\"term\":{\"Name" + suffix + "\":\"baz\"}}]" +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void Filter_OnNonStringReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);
            string suffix = esVersion >= 5
                ? String.Empty
                : Constants.RawSuffix;

            SearchResult result = _service.Search<TestPage>(searchText)
                .Filter(x => x.TestIntProp, 42)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"post_filter\": {" +
                "\"bool\": {" +
                "\"must\": [{" +
                "\"term\": {" +
                "\"TestIntProp" + suffix + "\": 42" +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void From_ReturnsExpectedJson()
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .From(42)
                .GetResults();

            string expected = RemoveWhitespace("\"from\": 42,");
            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void Skip_ReturnsExpectedJson()
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .From(42)
                .GetResults();

            string expected = RemoveWhitespace("\"from\": 42,");
            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void Size_ReturnsExpectedJson()
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Size(42)
                .GetResults();

            string expected = RemoveWhitespace("\"size\": 42,");
            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void Take_ReturnsExpectedJson()
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Size(42)
                .GetResults();

            string expected = RemoveWhitespace("\"size\": 42,");
            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(true)]
        [TestCase(false)]
        public void Fuzzy_ReturnsExpectedJson(bool auto)
        {
            string searchText = GetString(5);

            byte? length = auto ? null : (byte?)42;
            string fuzz = auto ? "AUTO" : "42";

            SearchResult result = _service.Search<TestPage>(searchText)
                .Fuzzy(length)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"query\": {" +
                "\"bool\": {" +
                "\"must\": [{" +
                "\"multi_match\": {" +
                "\"query\": \"" + searchText + "\"," +
                "\"fuzziness\": \"" + fuzz + "\"," +
                "\"lenient\": true," +
                "\"operator\": \"or\"");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void InField_ReturnsExpectedJson()
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .InField(x => x.TestProp)
                .GetResults();

            string expected = RemoveWhitespace("\"fields\": [\"TestProp\"]");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }



        [TestCase(42, 1337)]
        [TestCase(42.3, 1337.3)]
        public void Range_ReturnsExpectedJson(double from, double to)
        {
            string searchText = GetString(5);
            string fromString = from.ToString("#.0").Replace(',', '.');
            string toString = to.ToString("#.0").Replace(',', '.');

            SearchResult result = _service.Search<TestPage>(searchText)
                .Range(x => x.TestIntProp, from, to)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestIntProp\": {" +
                "\"gt\": " + fromString + "," +
                "\"lt\": " + toString +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [TestCase(42)]
        [TestCase(42.3)]
        public void Range_NullLessThan_ReturnsExpectedJson(double from)
        {
            string searchText = GetString(5);
            string fromString = from.ToString("#.0").Replace(',', '.');

            SearchResult result = _service.Search<TestPage>(searchText)
                .Range(x => x.TestIntProp, from)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestIntProp\": {" +
                "\"gt\": " + fromString +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }



        [TestCase(42, 1337)]
        [TestCase(42.3, 1337.3)]
        public void RangeInclusive_ReturnsExpectedJson(double from, double to)
        {
            string searchText = GetString(5);
            string fromString = from.ToString("#.0").Replace(',', '.');
            string toString = to.ToString("#.0").Replace(',', '.');

            SearchResult result = _service.Search<TestPage>(searchText)
                .RangeInclusive(x => x.TestIntProp, from, to)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestIntProp\": {" +
                "\"gte\": " + fromString + "," +
                "\"lte\": " + toString +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [TestCase(42, 1337)]
        [TestCase(-10, 42)]
        public void IntegerRange_ReturnsExpectedJson(int from, int to)
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Range(x => x.TestIntegerRange, from, to)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestIntegerRange\": {" +
                "\"relation\": \"intersects\"," +
                "\"gt\": " + from + "," +
                "\"lt\": " + to +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [TestCase(42, 1337)]
        [TestCase(-10, 42)]
        public void IntegerRangeInclusive_ReturnsExpectedJson(int from, int to)
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .RangeInclusive(x => x.TestIntegerRange, from, to)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestIntegerRange\": {" +
                "\"relation\": \"intersects\"," +
                "\"gte\": " + from + "," +
                "\"lte\": " + to +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void RangeDate_ReturnsExpectedJson()
        {
            string searchText = GetString(5);
            DateTime from = new DateTime(2001, 12, 31);
            DateTime to = new DateTime(2020, 2, 1);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Range(x => x.TestDateProp, from, to)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestDateProp\": {" +
                "\"gt\": \"" + from.ToString("s") + "\"," +
                "\"lt\": \"" + to.ToString("s") + "\"" +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void RangeDate_NullLessThan_ReturnsExpectedJson()
        {
            string searchText = GetString(5);
            DateTime from = new DateTime(2001, 12, 31);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Range(x => x.TestDateProp, from)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestDateProp\": {" +
                "\"gt\": \"" + from.ToString("s") + "\"" +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void RangeInclusiveDate_ReturnsExpectedJson()
        {
            string searchText = GetString(5);
            DateTime from = new DateTime(2001, 12, 31);
            DateTime to = new DateTime(2020, 2, 1);

            SearchResult result = _service.Search<TestPage>(searchText)
                .RangeInclusive(x => x.TestDateProp, from, to)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"range\": {" +
                "\"TestDateProp\": {" +
                "\"gte\": \"" + from.ToString("s") + "\"," +
                "\"lte\": \"" + to.ToString("s") + "\"" +
                "}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void SortBy_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);
            string suffix = esVersion >= 5
                ? Constants.KeywordSuffix
                : String.Empty;

            SearchResult result = _service.Search<TestPage>(searchText)
                .SortBy(x => x.TestProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"sort\": [{" +
                "\"TestProp" + suffix + "\": {" +
                "\"order\": \"asc\"" +
                "}}]");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void SortBy_ThenBy_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);
            string suffix = esVersion >= 5
                ? Constants.KeywordSuffix
                : String.Empty;

            SearchResult result = _service.Search<TestPage>(searchText)
                .SortBy(x => x.TestProp)
                .ThenBy(x => x.TestIntProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"sort\": [" +
                "{\"TestProp" + suffix + "\": {\"order\": \"asc\"}}," +
                "{\"TestIntProp\": {\"order\": \"asc\"}}" +
                "]");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }

        
        [TestCase(2)]
        [TestCase(5)]
        public void SortByNonString_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .SortBy(x => x.TestIntProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"sort\": [{" +
                "\"TestIntProp\": {" +
                "\"order\": \"asc\"" +
                "}}]");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void SortByDesc_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);
            string suffix = esVersion >= 5
                ? Constants.KeywordSuffix
                : String.Empty;

            SearchResult result = _service.Search<TestPage>(searchText)
                .SortByDescending(x => x.TestProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"sort\": [{" +
                "\"TestProp" + suffix + "\": {" +
                "\"order\": \"desc\"" +
                "}}]");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(2)]
        [TestCase(5)]
        public void SortByDescNonString_ReturnsExpectedJson(int esVersion)
        {
            Server.Info = new ServerInfo { ElasticVersion = new ServerInfo.InternalVersion { Number = esVersion + ".0.0.0" } };

            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .SortByDescending(x => x.TestIntProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"sort\": [{" +
                "\"TestIntProp\": {" +
                "\"order\": \"desc\"" +
                "}}]");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void WildCard_ReturnsExpectedJson()
        {
            string searchText = GetString(5);

            SearchResult result = _service.WildcardSearch<TestPage>(searchText)
                .InField(x => x.TestDateProp)
                .InField(x => x.TestIntProp)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"should\": [" +
                "{ \"wildcard\": { \"TestDateProp\": \"" + searchText + "\" } }," +
                "{ \"wildcard\": { \"TestIntProp\": \"" + searchText + "\" } }]," +
                "\"minimum_number_should_match\": 1");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }


        
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(666)]
        public void Decay_ReturnsExpectedJson(int scaleDays)
        {
            string searchText = GetString(5);
            TimeSpan scale = TimeSpan.FromDays(scaleDays);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Decay(x => x.StartPublish, scale)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"gauss\": {" +
                "\"StartPublish\": {" +
                "\"scale\": \"" + scale.TotalSeconds + "s\"," +
                "\"offset\": \"0s\"}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
            StringAssert.Contains("function_score", actual);
        }


        
        [TestCase(1)]
        [TestCase(7)]
        [TestCase(666)]
        public void Decay_ManualFieldName_ReturnsExpectedJson(int scaleDays)
        {
            string searchText = GetString(5);
            TimeSpan scale = TimeSpan.FromDays(scaleDays);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Decay("StartPublish", scale)
                .GetResults();

            string expected = RemoveWhitespace(
                "\"gauss\": {" +
                "\"StartPublish\": {" +
                "\"scale\": \"" + scale.TotalSeconds + "s\"," +
                "\"offset\": \"0s\"}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
            StringAssert.Contains("function_score", actual);
        }


        [Test]
        public void Decay_NoScaleSupplied_SetsScaleTo30Days()
        {
            string searchText = GetString(5);

            SearchResult result = _service.Search<TestPage>(searchText)
                .Decay("StartPublish")
                .GetResults();

            TimeSpan thirtyDays = TimeSpan.FromDays(30);

            string expected = RemoveWhitespace(
                "\"gauss\": {" +
                "\"StartPublish\": {" +
                "\"scale\": \"" + thirtyDays.TotalSeconds + "s\"," +
                "\"offset\": \"0s\"}}");

            string actual = RemoveWhitespace(result.Query);

            StringAssert.Contains(expected, actual);
        }
    }
}
