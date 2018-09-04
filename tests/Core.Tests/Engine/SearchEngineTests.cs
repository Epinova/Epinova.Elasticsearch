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
    public class SearchEngineTests : IDisposable
    {
        private TestableSearchEngine _engine;

        public SearchEngineTests()
        {
            Factory.SetupServiceLocator();    
        }


        public void Dispose()
        {
            _engine = null;
        }

        private void SetupEngineMock(string jsonFile)
        {
            _engine = new TestableSearchEngine(jsonFile);
        }

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

            SearchResult result = _engine.Query(new QueryRequest(new QuerySetup()), CultureInfo.InvariantCulture);

            Assert.NotNull(result);
        }


        [Fact]
        public void Query_NoHits_ReturnsNoHits()
        {
            SetupEngineMock("Results_No_Hits.json");

            SearchResult result = _engine.Query(new QueryRequest(new QuerySetup()), CultureInfo.InvariantCulture);

            Assert.Equal(0, result.Hits.Count());
        }


        [Fact]
        public void Query_NoHits_ReturnsNoFacets()
        {
            SetupEngineMock("Results_No_Hits.json");

            SearchResult result = _engine.Query(new QueryRequest(new QuerySetup()), CultureInfo.InvariantCulture);

            Assert.Equal(0, result.Facets.Length);
        }


        [Theory]
        [InlineData("Results_With_Hits_And_Facets.json")]
        [InlineData("Results_With_Only_Hits.json")]
        public void Query_WithHits_ReturnsHits(string jsonFile)
        {
            SetupEngineMock(jsonFile);

            SearchResult result = _engine.Query(new QueryRequest(new QuerySetup()), CultureInfo.InvariantCulture);

            Assert.NotEmpty(result.Hits);
        }


        [Theory]
        [InlineData("Results_With_Hits_And_Facets.json")]
        [InlineData("Results_With_Facets_Only.json")]
        public void Query_WithFacets_ReturnsFacets(string jsonFile)
        {
            SetupEngineMock(jsonFile);

            SearchResult result = _engine.Query(new QueryRequest(new QuerySetup()), CultureInfo.InvariantCulture);

            Assert.NotEmpty(result.Facets);
        }


        [Fact]
        public void Query_ReturnsCustomProperties()
        {
            SetupEngineMock("Results_With_Custom_Properties.json");
            DateTime date = DateTime.Parse("2015-03-31T23:01:04.2493062+02:00");
            const string text = "Lorem text";
            const double dec1 = 42.1;
            const long lng1 = 42;
            var arr1 = new long[] { 1, 2, 3 };
            var arr2 = new[] { "Foo", "Bar" };

            Indexing.Instance
                .ForType<TestPage>().IncludeField("Date", m => date)
                .ForType<TestPage>().IncludeField("Text", m => text)
                .ForType<TestPage>().IncludeField("Int", m => lng1)
                .ForType<TestPage>().IncludeField("Dec", m => dec1)
                .ForType<TestPage>().IncludeField("Array1", m => arr1)
                .ForType<TestPage>().IncludeField("Array2", m => arr2);

            SearchResult result = _engine.Query(new QueryRequest(new QuerySetup()), CultureInfo.InvariantCulture);

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
                .ForType<TestPage>().IncludeField<object>("Date", m => null)
                .ForType<TestPage>().IncludeField<object>("Text", m => null)
                .ForType<TestPage>().IncludeField<object>("Int", m => null)
                .ForType<TestPage>().IncludeField<object>("Dec", m => null)
                .ForType<TestPage>().IncludeField<object>("Array1", m => null)
                .ForType<TestPage>().IncludeField<object>("Array2", m => null);

            SearchResult result = _engine.Query(new QueryRequest(new QuerySetup()), CultureInfo.InvariantCulture);

            // Materialize ienumerable
            // ReSharper disable once UnusedVariable
            SearchHit[] hits = result.Hits.ToArray();
        }
    }
}
