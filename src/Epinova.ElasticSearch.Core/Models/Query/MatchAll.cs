using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class MatchAll : MatchBase
    {
        [JsonProperty(JsonNames.MatchAll)]
        public JObject Match { get; } = new JObject();
    }
}