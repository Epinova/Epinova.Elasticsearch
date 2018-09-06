using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models
{
    public class IndexItem
    {
        public IndexItem()
        {
            Suggest = new SuggestionItem();
        }

        [JsonExtensionData]
        internal IDictionary<string, JToken> UnmappedFields;

        [JsonProperty(DefaultFields.BestBets)]
        public string[] BestBets { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Lang { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public DateTime Updated { get; set; }
        public string Type { get; set; }
        public string[] Types { get; set; }
        public string Attachment { get; set; }
        public SuggestionItem Suggest { get; set; }

        [JsonProperty(DefaultFields.AttachmentData), JsonIgnore]
        public string AttachmentData { get; set; }

        public class SuggestionItem
        {
            [JsonProperty(JsonNames.Input)]
            public string[] Input { get; set; }
        }
    }
}