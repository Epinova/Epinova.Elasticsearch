using System.Globalization;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Utilities;
using Xunit;

namespace Core.Tests.Utilities
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class LanguageTests
    {
        [Theory]
        [InlineData("da", "da")]
        [InlineData("da-DK", "da")]
        [InlineData("en", "en")]
        [InlineData("en-GB", "en")]
        [InlineData("en-US", "en")]
        [InlineData("pt", "pt")]
        [InlineData("pt-BR", "pt")]
        [InlineData("no", "no")]
        [InlineData("nb-NO", "no")]
        [InlineData("nb-SJ", "no")]
        [InlineData("nb", "no")]
        [InlineData("nb-NO", "no")]
        [InlineData("nn", "no")]
        [InlineData("nn-NO", "no")]
        [InlineData("nl", "nl")]
        [InlineData("nl-AW", "nl")]
        [InlineData("nl-BE", "nl")]
        [InlineData("nl-BQ", "nl")]
        [InlineData("nl-CW", "nl")]
        [InlineData("nl-NL", "nl")]
        [InlineData("nl-SR", "nl")]
        [InlineData("nl-SX", "nl")]
        [InlineData("sv", "sv")]
        [InlineData("sv-AX", "sv")]
        [InlineData("sv-FI", "sv")]
        [InlineData("sv-SE", "sv")]
        [InlineData("en-LC", "en")]
        [InlineData("en-LR", "en")]
        [InlineData("en-LS", "en")]
        [InlineData("en-MG", "en")]
        [InlineData("en-MH", "en")]
        [InlineData("en-MO", "en")]
        [InlineData("en-MP", "en")]
        [InlineData("en-MS", "en")]
        [InlineData("en-MT", "en")]
        [InlineData("en-MU", "en")]
        [InlineData("en-MW", "en")]
        [InlineData("en-MY", "en")]
        [InlineData("en-NA", "en")]
        [InlineData("en-NF", "en")]
        [InlineData("fr", "fr")]
        [InlineData("fr-029", "fr")]
        [InlineData("fr-BE", "fr")]
        [InlineData("fr-BF", "fr")]
        [InlineData("de", "de")]
        [InlineData("de-AT", "de")]
        [InlineData("de-BE", "de")]
        [InlineData("es", "es")]
        [InlineData("es-419", "es")]
        [InlineData("es-AR", "es")]
        [InlineData("es-BO", "es")]
        public void GetLanguageCode_ReturnsCorrectCodeForCulture(string cultureString, string expectedCode)
        {
            string result = Language.GetLanguageCode(CultureInfo.CreateSpecificCulture(cultureString));

            Assert.Equal(expectedCode, result);
        }

        [Fact]
        public void GetLanguageCode_ReturnsAsteriskForInvariantCulture()
        {
            string result = Language.GetLanguageCode(CultureInfo.InvariantCulture);

            Assert.Equal("*", result);
        }

        [Theory]
        [InlineData("da", "danish")]
        [InlineData("en", "english")]
        [InlineData("fr", "french")]
        [InlineData("de", "german")]
        [InlineData("no", "norwegian")]
        [InlineData("es", "spanish")]
        [InlineData("sv", "swedish")]
        [InlineData("nl", "dutch")]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("foo", null)]
        public void GetLanguageAnalyzer_ReturnsCorrectAnalyzerForLanguage(string language, string expectedAnalyzer)
        {
            string result = Language.GetLanguageAnalyzer(language);

            Assert.Equal(expectedAnalyzer, result);
        }

        [Theory]
        [InlineData("da", true, "danish")]
        [InlineData("en", true, "english")]
        [InlineData("fr", true, "french")]
        [InlineData("de", true, "german")]
        [InlineData("no", true, "norwegian")]
        [InlineData("es", true, "spanish")]
        [InlineData("sv", true, "swedish")]
        [InlineData("nl", true, "dutch")]
        [InlineData("sv", false, null)]
        public void GetPropertyMapping_ReturnsCorrectMappingForLanguage(string language, bool isAnalyzable, string expectedAnalyzer)
        {
            IndexMappingProperty result = Language.GetPropertyMapping(language, typeof(string), isAnalyzable);

            Assert.Equal(expectedAnalyzer, result.Analyzer);
            Assert.Equal(nameof(MappingType.Text).ToLower(), result.Type);
        }
    }
}