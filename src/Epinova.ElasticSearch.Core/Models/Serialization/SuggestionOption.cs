using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class SuggestionOption
    {
        [JsonProperty(JsonNames.Text)]
        public string Text { get; set; }
    }
}