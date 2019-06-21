using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class Completion
    {
        [JsonProperty(JsonNames.Field)]
        public string Field { get; set; }

        [JsonProperty(JsonNames.Size)]
        public int Size { get; set; }

        [JsonProperty(JsonNames.SkipDuplicates)]
        public bool? SkipDuplicates { get; set; }
    }
}