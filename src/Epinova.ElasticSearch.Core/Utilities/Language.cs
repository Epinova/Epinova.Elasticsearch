using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web;
using Epinova.ElasticSearch.Core.Models.Mapping;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Language
    {
        /// <summary>
        /// For available language analyzers, see 
        /// https://www.elastic.co/guide/en/elasticsearch/reference/current/analysis-lang-analyzer.html
        /// </summary>
        private static readonly Dictionary<string, string> AnalyzerMappings = new Dictionary<string, string>
        {
            { "ar", "arabic" },
            { "am", "armenian" },
            { "eu", "basque" },
            { "bn", "bengali" },
            { "pt-br", "brazilian" },
            { "bg", "bulgarian" },
            { "ca", "catalan" },
            //{ "", "cjk" },    Don't know the language code for this. If you do feel free to add it.
            { "cs", "czech" },
            { "da", "danish" },
            { "nl", "dutch" },
            { "en", "english" },
            { "us", "english" },
            { "fi", "finnish" },
            { "fr", "french" },
            { "gl", "galican" },
            { "de", "german" },
            { "el", "greek" },
            { "hi", "hindi" },
            { "hu", "hungarian" },
            { "id", "indonesian" },
            { "ga", "irish" },
            { "it", "italian" },
            { "lv", "latvian" },
            { "lt", "lithuanian" },
            { "no", "norwegian" },
            { "fa", "persian" },
            { "pt", "portuguese" },
            { "ro", "romanian" },
            { "ru", "russian" },
            //{ "", "sorani" },     Don't know the language code for this. If you do feel free to add it.
            { "es", "spanish" },
            { "sv", "swedish" },
            { "tr", "turkish" },
            { "th", "thai" }
        };

#warning delete
        internal static string GetRequestLanguageCode()
            => GetLanguageCode(GetRequestLanguage());

#warning delete
        internal static string GetLanguageCode(CultureInfo cultureInfo)
        {
            //INFO: TwoLetterISOLanguageName returns "nb" for norwegian EPiServer-language

            if(CultureInfo.InvariantCulture.Equals(cultureInfo))
            {
                return "*";
            }

            // Return same code for normal and neutral languages, by looking at parent
            string code = String.Concat(cultureInfo.Name, cultureInfo.Parent.Name, cultureInfo.Parent.Parent.Name).Trim();

            return code.Substring(code.Length - 2, 2).ToLower();
        }

        internal static IndexMappingProperty GetPropertyMapping(string language, Type type, bool isAnalyzable)
        {
            string analyzer = null;

            if(isAnalyzable && language != null)
            {
                analyzer = GetLanguageAnalyzer(language);
            }

            IndexMappingProperty mapping = new IndexMappingProperty
            {
                Analyzer = analyzer,
                Type = Mapping.GetMappingTypeAsString(type)
            };

            return mapping;
        }

        internal static string GetSimpleLanguageAnalyzer(string language)
        {
            string analyzer = GetLanguageAnalyzer(language);

            if(analyzer == null)
            {
                return null;
            }

            return analyzer + "_simple";
        }

        internal static string GetLanguageAnalyzer(string language)
        {
            if(language == null)
            {
                return "fallback";
            }

            if(AnalyzerMappings.TryGetValue(language, out string analyzer))
            {
                return analyzer;
            }

            return "fallback";
        }

        internal static CultureInfo GetRequestLanguage()
        {
            var headers = HttpContext.Current?.Request?.Headers;

            if(headers?["X-EPiContentLanguage"] != null)
            {
                return CultureInfo.CreateSpecificCulture(headers["X-EPiContentLanguage"]);
            }

            return CultureInfo.InvariantCulture;
        }
    }
}