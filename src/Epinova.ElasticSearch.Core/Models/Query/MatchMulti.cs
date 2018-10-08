using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class MatchMulti : MatchBase
    {
        public MatchMulti(string query, List<string> fields, Operator @operator, string type = null, int? boost = null, string fuzziness = null, string analyzer = null)
        {
            MultiMatchQuery = new MultiMatchInternal
            {
                Query = query,
                Operator = @operator.ToString().ToLower(),
                Type = type,
                Boost = boost,
                Fields = fields,
                Fuzziness = fuzziness,
                Analyzer = analyzer
            };
        }

        public class MultiMatchInternal
        {
            [JsonProperty(JsonNames.Query)]
            public string Query { get; internal set; }

            [JsonProperty(JsonNames.Fuzziness)]
            public string Fuzziness { get; internal set; }

            [JsonProperty(JsonNames.Lenient)]
            public bool Lenient => true;

            [JsonProperty(JsonNames.Operator)]
            public string Operator { get; internal set; }

            [JsonProperty(JsonNames.Analyzer)]
            public string Analyzer { get; internal set; }

            [JsonProperty(JsonNames.Type)]
            public string Type { get; internal set; }

            [JsonProperty(JsonNames.Boost)]
            public int? Boost { get; internal set; }

            [JsonProperty(JsonNames.Fields)]
            public List<string> Fields { get; internal set; }
        }

        [JsonProperty(JsonNames.MultiMatch)]
        public MultiMatchInternal MultiMatchQuery { get; private set; }
    }
}