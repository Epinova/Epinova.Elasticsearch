using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class SuggestionsRootObject
    {
        [JsonProperty(JsonNames.Suggestions)]
        public Suggestions[] Suggestions { get; set; }

        [JsonProperty(JsonNames.Suggest)]
        public SuggestionsRootObject InnerRoot { get; set; }
    }
}