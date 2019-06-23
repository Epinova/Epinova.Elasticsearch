using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class HitBase<T>
    {
        [JsonProperty(JsonNames.Index)]
        public string Index { get; set; }

        [JsonProperty(JsonNames.HitType)]
        public string Type { get; set; }

        [JsonProperty(JsonNames.Id)]
        public string Id { get; set; }

        [JsonProperty(JsonNames.ScoreRoot)]
        public double Score { get; set; }

        [JsonProperty(JsonNames.Source)]
        public T Source { get; set; }

        [JsonIgnore]
        public string Highlight
        {
            get
            {
                if(HighlightList == null)
                {
                    return null;
                }

                KeyValuePair<string, string[]> firstHit = HighlightList.FirstOrDefault(h => h.Value != null && h.Value.Any());

                if(firstHit.Value == null || !firstHit.Value.Any())
                {
                    return null;
                }

                return firstHit.Value[0];
            }
        }

        [JsonProperty(JsonNames.Highlight)]
        private Dictionary<string, string[]> HighlightList { get; set; }
    }

    internal class Hit : HitBase<IndexItem>
    {
    }

    internal class CustomHit<T> : HitBase<T>
    {
    }
}