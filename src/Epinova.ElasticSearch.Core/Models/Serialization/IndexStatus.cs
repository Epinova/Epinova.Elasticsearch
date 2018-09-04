using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class IndexStatus
    {
        public string Status { get; set; }

        [JsonProperty(JsonNames.TimedOut)]
        public bool TimedOut { get; set; }
    }
}
