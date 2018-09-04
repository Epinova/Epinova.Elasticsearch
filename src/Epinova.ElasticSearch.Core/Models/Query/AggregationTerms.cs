using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class AggregationTerms
    {
        [JsonProperty(JsonNames.Field)]
        public string Field { get; set; }

        [JsonProperty(JsonNames.Size)]
        public int Size => 1000;
    }
}