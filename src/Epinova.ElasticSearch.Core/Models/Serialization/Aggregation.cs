using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class Aggregation
    {
        [JsonProperty(JsonNames.Buckets)]
        public Facet[] Facets { get; set; }
    }
}