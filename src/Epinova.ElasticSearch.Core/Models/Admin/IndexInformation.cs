using System;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Admin
{
    public class IndexInformation
    {
        [JsonProperty("uuid")]
        public string Uuid { get; private set; }

        [JsonProperty("health")]
        public string Health { get; private set; }

        [JsonProperty("status")]
        public string Status { get; private set; }

        [JsonIgnore]
        public string Type { get; internal set; }

        [JsonProperty("index")]
        public string Index { get; internal set; }

        [JsonIgnore]
        public string DisplayName { get; internal set; }

        [JsonIgnore]
        public int SortOrder { get; internal set; }

        [JsonIgnore]
        public string Tokenizer { get; internal set; }

        [JsonProperty("pri")]
        public int Pri { get; private set; }

        [JsonProperty("rep")]
        public int Rep { get; private set; }

        [JsonProperty("docs.count")]
        public int DocsCount { get; private set; }

        [JsonProperty("docs.deleted")]
        public int DocsDeleted { get; private set; }

        [JsonProperty("store.size")]
        public string StoreSize { get; private set; }

        [JsonProperty("pri.store.size")]
        public string PriStoreSize { get; private set; }

        [JsonIgnore]
        public string HealthColor
        {
            get
            {
                // Unknown, probably closed
                if(String.IsNullOrEmpty(Health))
                {
                    return "red";
                }

                // Poor contrast for "yellow"
                if(Health == "yellow")
                {
                    return "orange";
                }

                return Health;
            }
        }
    }
}
