using Epinova.ElasticSearch.Core.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Match : MatchBase
    {
        public class MatchInternal
        {
            [JsonProperty(DefaultFields.All)]
            public All AllQuery { get; internal set; }
        }

        public class All
        {
            [JsonProperty(JsonNames.Query)]
            public string Query { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty(JsonNames.Operator)]
            public Operator Operator { get; set; }

            [JsonProperty(JsonNames.Lenient)]
            public bool Lenient => true;
        }

        public Match(string query, Operator @operator)
        {
            MatchQuery = new MatchInternal
            {
                AllQuery = new All
                {
                    Query = query,
                    Operator = @operator
                }
            };
        }

        [JsonProperty(JsonNames.Match)]
        public MatchInternal MatchQuery { get; private set; }
    }
}
