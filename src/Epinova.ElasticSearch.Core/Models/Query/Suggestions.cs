using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class Suggestions
    {
        [JsonProperty(JsonNames.Text)]
        public string Text { get; set; }

        [JsonProperty(JsonNames.Completion)]
        public Completion Completion { get; set; }
    }
}