using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public class ErrorRootCause
    {
        [JsonProperty(JsonNames.Type)]
        public string Type { get; set; }

        [JsonProperty(JsonNames.Reason)]
        public string Reason { get; set; }

        [JsonProperty(JsonNames.StackTrace)]
        public string StackTrace { get; set; }
    }
}