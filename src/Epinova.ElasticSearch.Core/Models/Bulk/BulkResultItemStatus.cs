using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public class BulkResultItemStatus
    {
        public int Status { get; set; }

        [JsonProperty(JsonNames.Version)]
        public int Version { get; set; }
    }
}