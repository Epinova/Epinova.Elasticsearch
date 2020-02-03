using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class MappingPatterns
    {
        private static readonly string TextType = nameof(MappingType.Text).ToLower();
        private static readonly string IntType = nameof(MappingType.Integer).ToLower();
        private static readonly string LongType = nameof(MappingType.Long).ToLower();

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
                    Id = new { type = LongType },
                    StartPublish = new { type = "date" },
                    StopPublish = new { type = "date" },
                    Created = new { type = "date" },
                    Changed = new { type = "date" },
                    Indexed = new { type = "date" },
                    Name = new { type = TextType, fields = Fields },
                    _bestbets = new { type = TextType, fields = Fields },
                    ParentLink = new { type = LongType },
                    Path = new { type = LongType },
                    Lang = new { type = TextType },
                    DidYouMean = new { type = TextType, analyzer = languageName + "_suggest", fields = new { keyword = new { ignore_above = 8191, type = JsonNames.Keyword } } },
                    Suggest = SuggestMapping,
                    Type = new { type = TextType, analyzer = "raw", fields = Fields },
                    Types = new { type = TextType, analyzer = "raw" },
                    _acl = new { type = TextType, analyzer = "raw" },
                    _attachmentdata = new { type = TextType, fields = Fields },
                    attachment = GetAttachmentMapping(languageName)
                },
                _source = new
                {
                    excludes = new[]
                    {
                        DefaultFields.AttachmentData
                    }
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
                        type = TextType,
                        term_vector = "with_positions_offsets",
                        analyzer = languageName,
                        fields = Fields
                    },
                    title = new { type = TextType, fields = Fields },
                    language = new { type = TextType, fields = Fields },
                    content_type = new { type = TextType, fields = Fields },
                    content_length = new { type = LongType }
                }
            };
        }

        internal static dynamic GetCustomIndexMapping(string languageName)
        {
            return new
            {
                properties = new
                {
                    _bestbets = new { type = TextType },
                    Lang = new { type = TextType },
                    DidYouMean = new { type = TextType, analyzer = languageName + "_suggest", fields = new { raw = new { analyzer = "raw", type = TextType } } },
                    Suggest = SuggestMapping,
                    Type = new { type = TextType, analyzer = "raw", fields = Fields },
                    Types = new { type = TextType, analyzer = "raw" }
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