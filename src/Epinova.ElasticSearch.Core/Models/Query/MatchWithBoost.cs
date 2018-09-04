using Epinova.ElasticSearch.Core.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class MatchWithBoost : MatchBase
    {
        public MatchWithBoost(string field, string query, int boost, Operator @operator)
        {
            Match = new JObject(
                new JProperty(field, new JObject(
                    new JProperty(JsonNames.Query, query),
                    new JProperty(JsonNames.Boost, boost),
                    new JProperty(JsonNames.Lenient, true),
                    new JProperty(JsonNames.Operator, @operator.ToString().ToLower())
                    ))
                );
        }

        [JsonProperty(JsonNames.Match)]
        public JObject Match { get; private set; }
    }
}
