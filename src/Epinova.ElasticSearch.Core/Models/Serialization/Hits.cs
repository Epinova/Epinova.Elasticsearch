using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal abstract class HitsBase<T>
    {
        [JsonProperty(JsonNames.Hits)]
        public T[] HitArray { get; set; }

        [JsonProperty(JsonNames.Total)]
        public int Total { get; set; }

        [JsonProperty(JsonNames.MaxScore)]
        public double MaxScore { get; set; }
    }

    internal class Hits : HitsBase<Hit>
    {
    }

    internal class CustomHits<T> : HitsBase<CustomHit<T>>
    {
    }
}