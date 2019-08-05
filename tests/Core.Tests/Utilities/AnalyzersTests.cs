using Epinova.ElasticSearch.Core.Utilities;
using Newtonsoft.Json;
using Xunit;

namespace Core.Tests.Utilities
{
    public class AnalyzersTests
    {
        [Fact]
        public void GetAnalyzerSettings_SynonymsFilePath_SetsSynonymsPath()
        {
            var instance = Analyzers.GetAnalyzerSettings("en", "foo.txt");
            var result = JsonConvert.SerializeObject(instance);

            Assert.Contains("\"synonyms_path\":\"en_foo.txt\"", result);
        }

        [Fact]
        public void GetAnalyzerSettings_NoSynonymsFilePath_DoesNotSetSynonymsPath()
        {
            var instance = Analyzers.GetAnalyzerSettings("en", null);
            var result = JsonConvert.SerializeObject(instance);

            Assert.DoesNotContain("synonyms_path", result);
        }

        [Theory]
        [InlineData("fr", "light_french")]
        [InlineData("de", "light_german")]
        public void GetAnalyzerSettings_SpecialLanguage_SetsLightStemmer(string langCode, string expectedStemmer)
        {
            var instance = Analyzers.GetAnalyzerSettings(langCode, null);
            var result = JsonConvert.SerializeObject(instance);

            Assert.Contains(expectedStemmer, result);
        }

        [Fact]
        public void GetAnalyzerSettings_French_AddsElisionFilter()
        {
            var instance = Analyzers.GetAnalyzerSettings("fr", null);
            var result = JsonConvert.SerializeObject(instance);

            Assert.Contains("french_elision", result);
        }

        [Fact]
        public void GetAnalyzerSettings_German_AddsNormalizationFilter()
        {
            var instance = Analyzers.GetAnalyzerSettings("de", null);
            var result = JsonConvert.SerializeObject(instance);

            Assert.Contains("german_normalization", result);
        }

        [Fact]
        public void GetAnalyzerSettings_English_AddsPossessiveAndLightStemmer()
        {
            var instance = Analyzers.GetAnalyzerSettings("en", null);
            var result = JsonConvert.SerializeObject(instance);

            Assert.Contains("possessive_english", result);
            Assert.Contains("light_english", result);
        }
    }
}