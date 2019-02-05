using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public sealed class BulkResult
    {
        public int Took { get; set; }

        [JsonProperty("ingest_took")]
        public int IngestTook { get; set; }

        public bool Errors { get; set; }

        public BulkResultItem[] Items { get; set; } = new BulkResultItem[0];
    }
}