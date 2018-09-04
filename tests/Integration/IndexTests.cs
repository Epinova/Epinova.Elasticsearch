using System;
using System.Linq;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Utilities;
using Newtonsoft.Json;
using TestData;
using NUnit.Framework;
using EPiServer.ServiceLocation;
using Epinova.ElasticSearch.Core.Settings;

namespace Integration.Tests
{
    [TestFixture, Category("Integration")]
    public class IndexTests
    {
        private IElasticSearchSettings _settings;
        private Indexing _indexing;
        private Index _index;

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };


        [SetUp]
        public void Setup()
        {
            _settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            _indexing = new Indexing(_settings);
            _index = new Index(_settings, ElasticFixtureSettings.IndexName);
        }


        [Test]
        public void Initialize_CreatesCorrectIndex()
        {
            bool result = _indexing.IndexExists(ElasticFixtureSettings.IndexName);

            Assert.True(result);
        }


        [Test]
        public void Initialize_CreatesCorrectMapping()
        {
            string actual = JsonConvert.SerializeObject(
                Mapping.GetIndexMapping(
                    ElasticFixtureSettings.IndexType,
                    null,
                    null),
                JsonSettings);

            string expected = JsonConvert.SerializeObject(MappingPatterns.GetStandardIndexMappingWithoutType(ElasticFixtureSettings.LanguageName), JsonSettings)
                .Split(new[] { "\"Id\":" }, StringSplitOptions.None)[0]
                .TrimEnd('}');

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void Initialize_CreatesCorrectStandardSettings()
        {
            string actual = HttpClientHelper.GetString(_indexing.GetUri(ElasticFixtureSettings.IndexName, "_settings"));
            string expected = JsonConvert.SerializeObject(MappingPatterns.DefaultSettings.index, JsonSettings)
                .Trim('}', '{');

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void Initialize_CreatesRawTokenizer()
        {
            string actual = HttpClientHelper.GetString(_indexing.GetUri(ElasticFixtureSettings.IndexName, "_settings"));
            string expected =
                JsonConvert.SerializeObject(Analyzers.Raw.settings.analysis.analyzer, JsonSettings).TrimEnd('}');

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void Initialize_CreatesTriGramTokenizer()
        {
            string actual = HttpClientHelper.GetString(_indexing.GetUri(ElasticFixtureSettings.IndexName, "_settings"));
            string expected =
                JsonConvert.SerializeObject(Analyzers.TriGramTokenizer.settings.analysis.tokenizer, JsonSettings)
                    .TrimEnd('}');

            StringAssert.Contains(expected, actual);
        }


        [Test]
        public void Initialize_CreatesSynonymSettings()
        {
            string expectedFilterSynonym =
                JsonConvert.SerializeObject(
                    Analyzers.List.First(a => a.Key == ElasticFixtureSettings.LanguageName)
                        .Value.settings.analysis.filter.norwegian_synonym_filter, JsonSettings).TrimStart('{').TrimEnd('}');
            string expectedFilterStop =
                JsonConvert.SerializeObject(
                    Analyzers.List.First(a => a.Key == ElasticFixtureSettings.LanguageName)
                        .Value.settings.analysis.filter.norwegian_stop, JsonSettings).TrimStart('{').TrimEnd('}');
            string expectedFilterStemmer =
                JsonConvert.SerializeObject(
                    Analyzers.List.First(a => a.Key == ElasticFixtureSettings.LanguageName)
                        .Value.settings.analysis.filter.norwegian_stemmer, JsonSettings).TrimStart('{').TrimEnd('}');

            string expectedAnalyzer =
                JsonConvert.SerializeObject(
                    Analyzers.List.First(a => a.Key == ElasticFixtureSettings.LanguageName)
                        .Value.settings.analysis.analyzer, JsonSettings).TrimStart('{').TrimEnd('}');

            string actual = HttpClientHelper.GetString(_indexing.GetUri(ElasticFixtureSettings.IndexName, "_settings"))
                .Replace("\"true\"", "true")
                .Replace("\"false\"", "false");

            StringAssert.Contains(expectedFilterStemmer, actual);
            StringAssert.Contains(expectedFilterStop, actual);
            StringAssert.Contains(expectedFilterSynonym, actual);
            StringAssert.Contains(expectedAnalyzer, actual);
        }


        [Test]
        public void DeleteIndex_DeletesIndex()
        {
            _index.Initialize(ElasticFixtureSettings.IndexType);
            _indexing.DeleteIndex(ElasticFixtureSettings.IndexName);

            bool result = _indexing.IndexExists(ElasticFixtureSettings.IndexName);

            Assert.False(result);
        }
    }
}
