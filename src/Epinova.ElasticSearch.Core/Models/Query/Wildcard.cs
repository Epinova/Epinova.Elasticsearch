using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Wildcard : MatchBase
    {
        public Wildcard(string field, string query, sbyte boost = 0)
        {
            if(boost == 0)
            {
                WildcardQuery = new JObject(new JProperty(field, query));
            }
            else
            {
                WildcardQuery = new JObject(
                    new JProperty(
                        field,
                        new JObject(
                            new JProperty("value", query),
                            new JProperty("boost", boost)
                        )));
            }
        }

        [JsonProperty(JsonNames.Wildcard)]
        public JObject WildcardQuery { get; private set; }
    }
}