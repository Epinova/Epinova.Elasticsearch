using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public class Error
    {
        [JsonProperty(JsonNames.RootCause)]
        public ErrorRootCause[] RootCause { get; set; }

        [JsonProperty(JsonNames.Type)]
        public string Type { get; set; }

        [JsonProperty(JsonNames.Reason)]
        public string Reason { get; set; }

        [JsonProperty(JsonNames.Cause)]
        public Cause CausedBy { get; set; }

        public HeaderInfo Header { get; set; }

        [JsonProperty(JsonNames.StackTrace)]
        public string StackTrace { get; set; }


        public class Cause
        {
            [JsonProperty(JsonNames.Type)]
            public string Type { get; set; }

            [JsonProperty(JsonNames.Reason)]
            public string Reason { get; set; }
        }

        public class HeaderInfo
        {
            [JsonProperty("processor_type")]
            public string Processor { get; set; }
        }
    }
}