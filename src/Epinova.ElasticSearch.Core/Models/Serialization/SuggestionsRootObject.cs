using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Serialization
{
    internal class SuggestionsRootObject
    {
        [JsonProperty(JsonNames.Suggestions)]
        public Suggestions[] Suggestions { get; set; }
    }
}