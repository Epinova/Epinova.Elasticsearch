using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class Suggest
    {
        [JsonProperty(JsonNames.DidYouMean)]
        public DidYouMeanHit[] DidYouMean { get; set; }
    }
}