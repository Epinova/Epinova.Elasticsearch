using Epinova.ElasticSearch.Core.Models.Converters;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    [JsonConverter(typeof(BulkResultItemConverter))]
    public class BulkResultItem : BulkMetadataBase
    {
        public void Populate(BulkMetadataBase metadata, BulkResultItemStatus status)
        {
            Id = metadata.Id;
            Type = metadata.Type;
            Index = metadata.Index;
            Status = status.Status;
            Version = status.Version;
        }

        public Operation Operation { get; set; }

        private int Version { get; set; }

        public int Status { get; private set; }


        public override string ToString()
        {
            return $"Status: {Status}, Id: {Id}, Error: {Error?.Reason}, {Error?.CausedBy?.Type}, {Error?.CausedBy?.Reason}";
        }
    }
}
