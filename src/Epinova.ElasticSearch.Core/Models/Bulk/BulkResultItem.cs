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
            Error = metadata.Error;
        }

        public Operation Operation { get; set; }

        public int Version { get; set; }

        public int Status { get; private set; }


        public override string ToString()
        {
            var result = $"Status: {Status}\nId: {Id}\nError: {Error?.Reason}";

            if(Error?.Header?.Processor != null)
            {
                result += $"\nProcessor: {Error?.Header?.Processor}";
            }

            return result;
        }
    }
}