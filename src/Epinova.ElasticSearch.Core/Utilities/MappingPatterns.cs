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

        private static IndexMappingProperty SuggestMapping => new IndexMappingProperty { Type = "completion", Analyzer = "suggest" };

        private static readonly dynamic Fields = new { keyword = new { ignore_above = 256, type = JsonNames.Keyword } };

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
                properties = new
                {
                    Id = new { type = "long" },
                    StartPublish = new { type = "date" },
                    StopPublish = new { type = "date" },
                    Created = new { type = "date" },
                    Changed = new { type = "date" },
                    Indexed = new { type = "date" },
                    Name = new {
                        type = nameof(MappingType.Text).ToLower(),
                        fields = Fields
                    },
                    _bestbets = new {
                        type = nameof(MappingType.Text).ToLower(),
                        fields = Fields
                    },
                    ParentLink = new { type = "long" },
                    Path = new { type = "long" },
                    Lang = new { type = nameof(MappingType.Text).ToLower() },
                    DidYouMean = new { type = nameof(MappingType.Text).ToLower(), analyzer = languageName + "_suggest", fields = new { raw = new { analyzer = "raw", type = nameof(MappingType.Text).ToLower() } } },
                    Suggest = SuggestMapping,
                    Type = new { type = nameof(MappingType.Text).ToLower(), analyzer = "raw" },
                    Types = new { type = nameof(MappingType.Text).ToLower(), analyzer = "raw" },
                    _attachmentdata = new { type = nameof(MappingType.Text).ToLower() },
                    attachment = GetAttachmentMapping(languageName)
                }
            };
        }

        private static dynamic GetAttachmentMapping(string languageName)
        {
            return new
            {
                properties = new
                {
                    content = new
                    {
                        type = nameof(MappingType.Text).ToLower(),
                        term_vector = "with_positions_offsets",
                        store = true,
                        analyzer = languageName,
                        fields = new
                        {
                            keyword = new
                            {
                                type = "keyword",
                                ignore_above = 256
                            }
                        }
                    },
                    title = new
                    {
                        type = nameof(MappingType.Text).ToLower(),
                    },
                    language = new
                    {
                        type = nameof(MappingType.Text).ToLower(),
                    },
                    content_type = new
                    {
                        type = nameof(MappingType.Text).ToLower(),
                    },
                    content_length = new
                    {
                        type = nameof(MappingType.Integer).ToLower(),
                    }
                }
            };
        }
        
        internal static dynamic GetCustomIndexMapping(string languageName)
        {
            return new
            {
                properties = new
                {
                    _bestbets = new { type = nameof(MappingType.Text).ToLower() },
                    Lang = new { type = nameof(MappingType.Text).ToLower() },
                    DidYouMean = new { type = nameof(MappingType.Text).ToLower(), analyzer = languageName + "_suggest", fields = new { raw = new { analyzer = "raw", type = nameof(MappingType.Text).ToLower() } } },
                    Suggest = SuggestMapping,
                    Type = new { type = nameof(MappingType.Text).ToLower(), analyzer = "raw" },
                    Types = new { type = nameof(MappingType.Text).ToLower(), analyzer = "raw" }
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