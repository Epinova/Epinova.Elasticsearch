using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Epinova.ElasticSearch.Core.Models.Mapping;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Language
    {
        internal static ILanguageBranchRepository _languageBranchRepository = ServiceLocator.Current.GetInstance<ILanguageBranchRepository>();

        /// <summary>
        /// For available language analyzers, see 
        /// https://www.elastic.co/guide/en/elasticsearch/reference/current/analysis-lang-analyzer.html
        /// </summary>
        private static readonly Dictionary<string, string> AnalyzerMappings = new Dictionary<string, string>
        {
            { "da", "danish" },
            { "en", "english" },
			{ "us", "english" },
            { "fr", "french" },
            { "de", "german" },
            { "no", "norwegian" },
            { "nl", "dutch" },
            { "es", "spanish" },
            { "sv", "swedish" }
        };

        internal static string GetRequestLanguageCode()
            => GetLanguageCode(GetRequestLanguage());

        internal static string GetLanguageCode(CultureInfo cultureInfo)
        {
            //INFO: TwoLetterISOLanguageName returns "nb" for norwegian EPiServer-language

            if(CultureInfo.InvariantCulture.Equals(cultureInfo))
            {
                return "*";
            }

            var enabledLanguages = _languageBranchRepository.ListEnabled();

            if(enabledLanguages == null)
            {
                return "*";
            }

            //check if currentCulture is one of the enabled languages in episerver.
            //if so return it
            foreach(var item in enabledLanguages)
            {
                CultureInfo tmpCultureInfo = new CultureInfo(item.LanguageID);
                if(tmpCultureInfo.Name == CultureInfo.CurrentCulture.Name)
                    return item.LanguageID.ToLower();
            }

            //if not any hits in the foreach above: 
            //check if the parent currentCulture is one of the enabled languages in episerver.
            //if so return it
            foreach(var item in enabledLanguages)
            {
                CultureInfo tmpCultureInfo = new CultureInfo(item.LanguageID);
                if(tmpCultureInfo.Name == CultureInfo.CurrentCulture.Parent.Name)
                    return item.LanguageID.ToLower();
            }

            return "*";
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
                return null;
            }

            if(AnalyzerMappings.TryGetValue(language, out string analyzer))
            {
                return analyzer;
            }

            return null;
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
