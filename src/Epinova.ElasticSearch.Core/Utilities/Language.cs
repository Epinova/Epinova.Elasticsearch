using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            { "da", "danish" },
            { "en", "english" },
            { "fr", "french" },
            { "de", "german" },
            { "no", "norwegian" },
            { "nl", "dutch" },
            { "es", "spanish" },
            { "sv", "swedish" }
        };

        internal static string GetRequestLanguageCode()
        {
            return GetLanguageCode(GetRequestLanguage());
        }

        internal static string GetLanguageCode(CultureInfo cultureInfo)
        {
            //INFO: TwoLetterISOLanguageName returns "nb" for norwegian EPiServer-language

            if (CultureInfo.InvariantCulture.Equals(cultureInfo))
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

            if (isAnalyzable && language != null)
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

            if (analyzer == null)
            {
                return null;
            }

            return analyzer + "_simple";
        }

        internal static string GetLanguageAnalyzer(string language)
        {
            if (language == null)
            {
                return null;
            }

            if (AnalyzerMappings.TryGetValue(language, out string analyzer))
            {
                return analyzer;
            }

            return null;
        }

        internal static CultureInfo GetRequestLanguage()
        {
            var headers = HttpContext.Current?.Request?.Headers;

            if (headers?.AllKeys.Contains("X-EPiContentLanguage") == true)
            {
                return CultureInfo.CreateSpecificCulture(headers["X-EPiContentLanguage"]);
            }

            return CultureInfo.InvariantCulture;
        }
    }
}
