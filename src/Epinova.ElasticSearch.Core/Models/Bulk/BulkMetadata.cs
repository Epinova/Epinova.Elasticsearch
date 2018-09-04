using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public class BulkMetadata : BulkMetadataBase
    {
        [JsonIgnore]
        public Operation Operation { get; set; }
    }
}