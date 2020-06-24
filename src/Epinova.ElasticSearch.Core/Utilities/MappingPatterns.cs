using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class MappingPatterns
    {
        private static readonly string _textType = nameof(MappingType.Text).ToLower();
        private static readonly string _longType = nameof(MappingType.Long).ToLower();

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
                    Id = new { type = _longType },
                    StartPublish = new { type = "date" },
                    StopPublish = new { type = "date" },
                    Created = new { type = "date" },
                    Changed = new { type = "date" },
                    Indexed = new { type = "date" },
                    Name = new { type = _textType, fields = Fields },
                    _bestbets = new { type = _textType, fields = Fields },
                    ParentLink = new { type = _longType },
                    Path = new { type = _longType },
                    Lang = new { type = _textType },
                    DidYouMean = new { type = _textType, analyzer = languageName + "_suggest", fields = new { keyword = new { ignore_above = 8191, type = JsonNames.Keyword } } },
                    Suggest = SuggestMapping,
                    Type = new { type = _textType, analyzer = "raw", fields = Fields },
                    Types = new { type = _textType, analyzer = "raw" },
                    _acl = new { type = _textType, analyzer = "raw" },
                    _attachmentdata = new { type = _textType, fields = Fields },
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
                        type = _textType,
                        term_vector = "with_positions_offsets",
                        analyzer = languageName,
                        fields = Fields
                    },
                    title = new { type = _textType, fields = Fields },
                    language = new { type = _textType, fields = Fields },
                    content_type = new { type = _textType, fields = Fields },
                    content_length = new { type = _longType }
                }
            };
        }

        internal static dynamic GetCustomIndexMapping(string languageName)
        {
            return new
            {
                properties = new
                {
                    _bestbets = new { type = _textType },
                    Lang = new { type = _textType },
                    DidYouMean = new { type = _textType, analyzer = languageName + "_suggest", fields = new { raw = new { analyzer = "raw", type = _textType } } },
                    Suggest = SuggestMapping,
                    Type = new { type = _textType, analyzer = "raw", fields = Fields },
                    Types = new { type = _textType, analyzer = "raw" }
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