using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class Suggestions
    {
        [JsonProperty(JsonNames.Options)]
        public SuggestionOption[] Options { get; set; }
    }
}