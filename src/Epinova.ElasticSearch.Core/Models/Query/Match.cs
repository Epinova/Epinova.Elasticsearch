using Epinova.ElasticSearch.Core.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Match : MatchBase
    {
        public class MatchInternal
        {
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
            MatchQuery = new MatchInternal();
        }

        [JsonProperty(JsonNames.Match)]
        public MatchInternal MatchQuery { get; private set; }
    }
}
