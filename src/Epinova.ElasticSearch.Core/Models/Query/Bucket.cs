using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Bucket
    {
        public Bucket(string field)
        {
            Terms = new AggregationTerms { Field = field };
        }

        [JsonProperty(JsonNames.Terms)]
        public AggregationTerms Terms { get; set; }
    }
}