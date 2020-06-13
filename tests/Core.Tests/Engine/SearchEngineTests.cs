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
            var date = DateTime.Parse("2015-03-31T23:01:04.2493062+02:00");
            const string text = "Lorem text";
            const double dec1 = 42.1;
            const long lng1 = 42;
            var arr1 = new long[] { 1, 2, 3 };
            var arr2 = new[] { "Foo", "Bar" };

            Indexing.Instance
                .ForType<TestPage>().IncludeField("Date", _ => date)
                .ForType<TestPage>().IncludeField("Text", _ => text)
                .ForType<TestPage>().IncludeField("Int", _ => lng1)
                .ForType<TestPage>().IncludeField("Dec", _ => dec1)
                .ForType<TestPage>().IncludeField("Array1", _ => arr1)
                .ForType<TestPage>().IncludeField("Array2", _ => arr2);

            SearchResult result = _engine.Query(_dummyQuery, CultureInfo.InvariantCulture);

            Assert.Equal(date, result.Hits.First().CustomProperties["Date"]);
            Assert.Equal(text, result.Hits.First().CustomProperties["Text"]);
            Assert.Equal(lng1, result.Hits.First().CustomProperties["Int"]);
            Assert.Equal(dec1, result.Hits.First().CustomProperties["Dec"]);
            Assert.True(Factory.ArrayEquals(arr1, result.Hits.First().CustomProperties["Array1"] as IEnumerable<object>));
            Assert.True(Factory.ArrayEquals(arr2, result.Hits.First().CustomProperties["Array2"] as IEnumerable<object>));
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
