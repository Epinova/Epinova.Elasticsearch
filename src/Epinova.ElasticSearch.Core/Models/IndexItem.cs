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

        public string[] _bestbets { get; set; }
        public int Id { get; set; }
        public int ParentId { get; set; }
        public string Lang { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public DateTime Updated { get; set; }
        public string Type { get; set; }
        public string[] Types { get; set; }

        public Attachment attachment { get; set; }
        public SuggestionItem Suggest { get; set; }

        [JsonIgnore]
        public string _attachmentdata { get; set; }

        public class SuggestionItem
        {
            [JsonProperty(JsonNames.Input)]
            public string[] Input { get; set; }
        }

        public class Attachment
        {
            [JsonProperty("date")]
            public DateTime Date { get; set; }

            [JsonProperty("content_type")]
            public string ContentType { get; set; }

            [JsonProperty("content_length")]
            public int ContentLength { get; set; }

            [JsonProperty("language")]
            public string Language { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("author")]
            public string Author { get; set; }

            //[JsonProperty("content")]
            //public string Content { get; set; }
            
            //[JsonProperty("keywords")]
            //public string Keywords { get; set; }
            
            //[JsonProperty("modified")]
            //public DateTime Modified { get; set; }
            
            //[JsonProperty("format")]
            //public string Format { get; set; }
            
            //[JsonProperty("identifier")]
            //public string Identifier { get; set; }
            
            //[JsonProperty("contributor")]
            //public string Contributor { get; set; }
            
            //[JsonProperty("coverage")]
            //public string Coverage { get; set; }
            
            //[JsonProperty("modifier")]
            //public string Modifier { get; set; }
            
            //[JsonProperty("creator_tool")]
            //public string CreatorTool { get; set; }
            
            //[JsonProperty("publisher")]
            //public string Publisher { get; set; }
            
            //[JsonProperty("relation")]
            //public string Relation { get; set; }
            
            //[JsonProperty("rights")]
            //public string Rights { get; set; }
            
            //[JsonProperty("source")]
            //public string Source { get; set; }
            
            //[JsonProperty("type")]
            //public string Type { get; set; }
            
            //[JsonProperty("description")]
            //public string Description { get; set; }
            
            //[JsonProperty("print_date")]
            //public DateTime PrintDate { get; set; }
            
            //[JsonProperty("metadata_date")]
            //public DateTime MetaDataDate { get; set; }
            
            //[JsonProperty("latitude")]
            //public decimal Latitude { get; set; }
            
            //[JsonProperty("longitude")]
            //public decimal Longitude { get; set; }
            
            //[JsonProperty("altitude")]
            //public decimal Altitude { get; set; }
            
            //[JsonProperty("rating")]
            //public string Rating { get; set; }
            
            //[JsonProperty("comments")]
            //public string Comments { get; set; }
        }
    }
}