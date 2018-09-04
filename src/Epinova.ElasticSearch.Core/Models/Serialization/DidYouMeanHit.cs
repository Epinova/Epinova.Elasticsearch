using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class DidYouMeanHit
    {
        [JsonProperty(JsonNames.Text)]
        public string Text { get; set; }

        [JsonProperty(JsonNames.Options)]
        public DidYouMeanOption[] Options { get; set; }
    }
}
