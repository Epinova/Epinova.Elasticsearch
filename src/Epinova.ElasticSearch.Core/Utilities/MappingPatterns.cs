using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class MappingPatterns
    {
        internal static dynamic GetTokenizerTemplate(string language, string tokenizer)
        {
            string analyzer = Language.GetLanguageAnalyzer(language);

            dynamic lang = new System.Dynamic.ExpandoObject();
            ((IDictionary<string, object>)lang).Add(analyzer, new { tokenizer });

            dynamic body = new System.Dynamic.ExpandoObject();
            ((IDictionary<string, object>)body).Add("index", new { analysis = new { analyzer = lang } });

            return body;
        }


        internal static readonly dynamic DefaultSettings = new { index = new { refresh_interval = "-1" } };

        internal static readonly dynamic IndexClosing = new
        {
            persistent = new
            {
                cluster = new
                {
                    indices = new
                    {
                        close = new
                        {
                            enable = true
                        }
                    }
                }
            }
        };

        private static IndexMappingProperty SuggestMapping
        {
            get
            {
                var suggestMapping = new IndexMappingProperty
                {
                    Type = "completion",
                    Analyzer = "suggest"
                };
                
                // Payloads deprecated in v5
                if (Server.Info.Version.Major < 5)
                    suggestMapping.Payloads = false;

                return suggestMapping;
            }
        }


        internal static readonly string StringType = Server.Info.Version.Major >= 5
            ? MappingType.Text.ToString().ToLower()
            : MappingType.String.ToString().ToLower();


        internal static readonly dynamic Fields = Server.Info.Version.Major >= 5
            ? new { keyword = new { ignore_above = 256, type = JsonNames.Keyword } }
            : null;


        internal static string GetDisableDynamicMapping(string typeName)
        {
            return "{ " +
                   "    \"" + typeName + "\": { " +
                   "        \"dynamic\": false" +
                   "    }" +
                   "}";
        }

        internal static dynamic GetStandardIndexMappingWithoutType(string languageName)
        {
            return new
            {
                _all = new {analyzer = "snowball"},
                properties = new
                {
                    Attachment = new
                    {
                        type = StringType,
                        fields = new
                        {
                            content = new
                            {
                                type = StringType,
                                term_vector = "with_positions_offsets",
                                store = true
                            }
                        }
                    },
                    Id = new { type = "long", include_in_all = false },
                    _bestbets = new {
                        type = StringType,
                        fields = Fields
                    },
                    ParentLink = new { type = "long", include_in_all = false },
                    Path = new { type = "long", include_in_all = false },
                    Lang = new { type = StringType },
                    DidYouMean = new { type = StringType, analyzer = languageName + "_suggest", fields = new { raw = new { analyzer = "raw", type = StringType } } },
                    Suggest = SuggestMapping,
                    Type = new { type = StringType, analyzer = "raw" },
                    Types = new { type = StringType, analyzer = "raw" }
                }
            };
        }

        internal static dynamic GetCustomIndexMapping(string languageName)
        {
            return new
            {
                _all = new { analyzer = "snowball" },
                properties = new
                {
                    _bestbets = new { type = StringType },
                    Lang = new { type = StringType },
                    DidYouMean = new { type = StringType, analyzer = languageName + "_suggest", fields = new { raw = new { analyzer = "raw", type = StringType } } },
                    Suggest = SuggestMapping,
                    Type = new { type = StringType, analyzer = "raw" },
                    Types = new { type = StringType, analyzer = "raw" }
                }
            };
        }

        internal static dynamic GetStandardIndexMapping(string languageName)
        {
            return new
            {
                Epinova_ElasticSearch_Core_Models_IndexItem =
                    GetStandardIndexMappingWithoutType(languageName)
            };
        }
    }
}