using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Query;
using Moq;
using TestData;
using Xunit;

namespace Core.Tests.Engine
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class SearchEngineTests : IDisposable, IClassFixture<ServiceLocatorFixture>
    {
        private readonly ServiceLocatorFixture _fixture;
        private TestableSearchEngine _engine;
        private readonly QueryRequest _dummyQuery;

        public SearchEngineTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;
            _dummyQuery = new QueryRequest(new QuerySetup { SearchText = "foo" });
        }

        public void Dispose() => _engine = null;

        private void SetupEngineMock(string jsonFile) =>
            _engine = new TestableSearchEngine(
                jsonFile,
                _fixture.ServiceLocationMock.ServerInfoMock.Object,
                _fixture.ServiceLocationMock.SettingsMock.Object,
                _fixture.ServiceLocationMock.HttpClientMock.Object);

        [Fact]
        public void GetSuggestions_InvalidResult_ReturnEmptyArray()
        {
            SetupEngineMock("I_Dont_Exists.json");

            string[] result = _engine.GetSuggestions(It.IsAny<SuggestRequest>(), CultureInfo.CurrentCulture);

            Assert.Empty(result);
        }

        [Fact]
        public void GetSuggestions_ValidResult_DoesNotReturnNull()
        {
            SetupEngineMock("Suggestions_2hits.json");

            string[] result = _engine.GetSuggestions(It.IsAny<SuggestRequest>(), CultureInfo.CurrentCulture);

            Assert.NotNull(result);
        }

        [Fact]
        public void GetSuggestions_ValidResult_ReturnsCorrectAmount()
        {
            SetupEngineMock("Suggestions_2hits.json");

            string[] result = _engine.GetSuggestions(It.IsAny<SuggestRequest>(), CultureInfo.CurrentCulture);

            Assert.True(result.Length == 2);
        }

        [Fact]
        public void Query_Result_IsNeverNull()
        {
            SetupEngineMock("I_Dont_Exists.json");

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            Assert.NotNull(result);
        }

        [Fact]
        public void Query_NoHits_ReturnsNoHits()
        {
            SetupEngineMock("Results_No_Hits.json");

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            Assert.Equal(0, result.Hits.Count());
        }

        [Fact]
        public void Query_NoHits_ReturnsNoFacets()
        {
            SetupEngineMock("Results_No_Hits.json");

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            Assert.Equal(0, result.Facets.Length);
        }

        [Theory]
        [InlineData("Results_With_Hits_And_Facets.json")]
        [InlineData("Results_With_Only_Hits.json")]
        public void Query_WithHits_ReturnsHits(string jsonFile)
        {
            SetupEngineMock(jsonFile);

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            Assert.NotEmpty(result.Hits);
        }

        [Theory]
        [InlineData("Results_With_Hits_And_Facets.json")]
        [InlineData("Results_With_Facets_Only.json")]
        public void Query_WithFacets_ReturnsFacets(string jsonFile)
        {
            SetupEngineMock(jsonFile);

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            Assert.NotEmpty(result.Facets);
        }

        [Fact]
        public void Query_ReturnsCustomProperties()
        {
            SetupEngineMock("Results_With_Custom_Properties.json");
            DateTime date = DateTime.Parse("2015-03-31T23:01:04.2493062+02:00");
            const string text = "Lorem text";
            const Int32 int1 = 327;
            decimal dec1 = new decimal(42.1);
            const double dou1 = 42.1;
            const long lng1 = 4221344;
            long[] arr1 = { 1, 2, 3 };
            string[] arr2 = { "Foo", "Bar" };

            Dictionary<string, string> dictString = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "1" }};
            Dictionary<string, int> dictInt = new Dictionary<string, int> { { "key2", 1 }, { "key3", 2 } };
            Dictionary<string, string[]> dictStringArray = new Dictionary<string, string[]> {{ "key1", new[] { "1", "2" } }, { "key2", new[] { "3", "4" } }};
            Dictionary<string, int[]> dictIntArray = new Dictionary<string, int[]> {{ "key1", new[] { 1, 2 } }, { "key2", new[] { 3, 4 } }};
            
            Indexing.Instance
                .ForType<TestPage>().IncludeField("Date", _ => date)
                .ForType<TestPage>().IncludeField("Text", _ => text)
                .ForType<TestPage>().IncludeField("Integer", _ => int1)
                .ForType<TestPage>().IncludeField("Decimal", _ => dec1)
                .ForType<TestPage>().IncludeField("Double", _ => dou1)
                .ForType<TestPage>().IncludeField("Long", _ => lng1)
                .ForType<TestPage>().IncludeField("Array1", _ => arr1)
                .ForType<TestPage>().IncludeField("Array2", _ => arr2)
                .ForType<TestPage>().IncludeField("DictionaryString", _ => dictString)
                .ForType<TestPage>().IncludeField("DictionaryInt", _ => dictInt)
                .ForType<TestPage>().IncludeField("DictionaryStringArray", _ => dictStringArray)
                .ForType<TestPage>().IncludeField("DictionaryIntArray", _ => dictIntArray);

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            SearchHit searchHit = result.Hits.First();

            Assert.Equal(date, searchHit.CustomProperties["Date"]);
            Assert.Equal(text, searchHit.CustomProperties["Text"]);
            Assert.Equal(int1, Convert.ToInt32(searchHit.CustomProperties["Integer"]));
            Assert.Equal(dec1, Convert.ToDecimal(searchHit.CustomProperties["Decimal"]));
            Assert.Equal(dou1, searchHit.CustomProperties["Double"]);
            Assert.Equal(lng1, searchHit.CustomProperties["Long"]);
            Assert.True(Factory.ArrayEquals(arr1, searchHit.CustomProperties["Array1"] as IEnumerable<long>));
            Assert.True(Factory.ArrayEquals(arr2, searchHit.CustomProperties["Array2"] as IEnumerable<string>));
            Assert.True(Factory.ArrayEquals(dictString, searchHit.CustomProperties["DictionaryString"] as Dictionary<string, string>));
            Assert.True(Factory.ArrayEquals(dictInt, searchHit.CustomProperties["DictionaryInt"] as Dictionary<string, int>));
        }

        [Fact]
        public void Query_CustomPropertiesWithNulls_DoesNotThrow()
        {
            SetupEngineMock("Results_With_Custom_Properties_NullValues.json");

            Indexing.CustomProperties.Clear();

            Indexing.Instance
                .ForType<TestPage>().IncludeField<object>("Date", _ => null)
                .ForType<TestPage>().IncludeField<object>("Text", _ => null)
                .ForType<TestPage>().IncludeField<object>("Int", _ => null)
                .ForType<TestPage>().IncludeField<object>("Dec", _ => null)
                .ForType<TestPage>().IncludeField<object>("Array1", _ => null)
                .ForType<TestPage>().IncludeField<object>("Array2", _ => null);

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            // Materialize ienumerable
            SearchHit[] hits = result.Hits.ToArray();
        }
    }
}
