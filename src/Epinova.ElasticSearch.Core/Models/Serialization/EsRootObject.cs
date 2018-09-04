using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class EsRootObjectBase<T>
    {
        public int Took { get; set; }

        [JsonProperty(JsonNames.TimedOut)]
        public bool TimedOut { get; set; }

        [JsonProperty(JsonNames.Aggregations)]
        public Dictionary<string, Aggregation> Aggregations { get; set; }

        [JsonProperty(JsonNames.Hits)]
        public T Hits { get; set; }

        [JsonProperty(JsonNames.Suggest)]
        public Suggest Suggest { get; set; }
    }

    internal class EsRootObject : EsRootObjectBase<Hits>
    {
    }

    internal class EsCustomRootObject<T> : EsRootObjectBase<CustomHits<T>>
    {
    }
}