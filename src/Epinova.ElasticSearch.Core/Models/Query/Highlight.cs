using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class Highlight
    {
        public Highlight()
        {
            Fields = new Dictionary<string, object>();
        }

        [JsonProperty(JsonNames.Order)]
        public string Order => "score";

        [JsonProperty(JsonNames.PreTags)]
        public string[] PreTags
        {
            get
            {
                string tag = Conventions.Indexing.HighlightTag;
                if(String.IsNullOrEmpty(tag))
                {
                    return new[] { String.Empty };
                }

                return new[] { $"<{tag}>" };
            }
        }

        [JsonProperty(JsonNames.PostTags)]
        public string[] PostTags
        {
            get
            {
                string tag = Conventions.Indexing.HighlightTag;
                if(String.IsNullOrEmpty(tag))
                {
                    return new[] { String.Empty };
                }

                return new[] { $"</{tag}>" };
            }
        }

        [JsonProperty(JsonNames.Fields)]
        public Dictionary<string, object> Fields { get; set; }
    }
}