using System;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Admin
{
    public class HealthInformation
    {
        [JsonProperty("epoch")]
        public long Epoch { get; private set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; private set; }

        [JsonProperty("cluster")]
        public string Cluster { get; private set; }

        [JsonProperty("status")]
        public string Status { get; private set; }

        [JsonProperty("node.total")]
        public int NodeTotal { get; private set; }

        [JsonProperty("node.data")]
        public int NodeData { get; private set; }

        [JsonProperty("shards")]
        public int Shards { get; private set; }

        [JsonProperty("pri")]
        public int Pri { get; private set; }

        [JsonProperty("relo")]
        public int Relo { get; private set; }

        [JsonProperty("init")]
        public int Init { get; private set; }

        [JsonProperty("unassign")]
        public int Unassign { get; private set; }

        [JsonProperty("pending_tasks")]
        public int PendingTasks { get; private set; }

        [JsonProperty("active_shards_percent")]
        public string ActiveShardsPercent { get; private set; }

        [JsonIgnore]
        public string StatusColor
        {
            get
            {
                // Unknown, probably closed
                if(String.IsNullOrEmpty(Status))
                {
                    return "red";
                }

                // Poor contrast for "yellow"
                if(Status == "yellow")
                {
                    return "orange";
                }

                return Status;
            }
        }
    }
}