using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    public class DidYouMeanOption
    {
        [JsonProperty(JsonNames.Text)]
        public string Text { get; set; }

        [JsonProperty(JsonNames.Score)]
        public double Score { get; set; }
    }
}