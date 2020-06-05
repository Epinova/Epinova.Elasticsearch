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

        [JsonIgnore]
        public string Ip => Server.Info.Version.Major >= 5 ? IpInternal5 : IpInternal2;

        [JsonProperty("i")]
        internal string IpInternal2 { get; set; }

        [JsonProperty("http")]
        internal string IpInternal5 { get; set; }

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