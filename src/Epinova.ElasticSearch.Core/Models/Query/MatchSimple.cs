using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class MatchSimple : MatchBase
    {
        public MatchSimple(string field, string query)
        {
            Match = new JObject(new JProperty(field, query));
        }

        [JsonProperty(JsonNames.Match)]
        public JObject Match { get; private set; }
    }
}