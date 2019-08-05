using System;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Admin
{
    public class ServerInfo
    {
        public string Name { get; set; }

        [JsonProperty(JsonNames.ClusterName)]
        public string Cluster { get; set; }

        [JsonIgnore]
        public Version Version => new Version(ElasticVersion.Number);

        [JsonIgnore]
        public Version LuceneVersion => new Version(ElasticVersion.LuceneVersion);

        [JsonProperty(JsonNames.NodeVersion)]
        internal InternalVersion ElasticVersion { get; set; }

        internal class InternalVersion
        {
            public string Number { get; set; }

            [JsonProperty(JsonNames.LuceneVersion)]
            public string LuceneVersion { get; set; }
        }

        public override string ToString()
            => $"{Name} ({Cluster}): v{Version}";
    }
}