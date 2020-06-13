using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Admin
{
    public class Node
    {
        [JsonProperty("n")]
        public string Name { get; private set; }

        [JsonProperty("m")]
        private string MasterInternal { get; set; }

        [JsonIgnore]
        public bool Master => MasterInternal == "*";

        [JsonProperty("v")]
        public string Version { get; private set; }

        [JsonProperty("http")]
        public string Ip { get; set; }

        [JsonProperty("d")]
        public string HddAvailable { get; private set; }

        [JsonProperty("rc")]
        public string MemoryCurrent { get; private set; }

        [JsonProperty("rm")]
        public string MemoryTotal { get; private set; }

        [JsonProperty("u")]
        public string Uptime { get; private set; }
    }
}