using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class MatchSimpleQueryString : MatchBase
    {
        public MatchSimpleQueryString(string query, List<string> fields, Operator @operator, string analyzer = null)
        {
            SimpleQueryString = new SimpleQueryStringInternal
            {
                Query = query,
                DefaultOperator = @operator.ToString().ToLower(),
                Fields = fields,
                Analyzer = analyzer
            };
        }

        public class SimpleQueryStringInternal
        {
            [JsonProperty(JsonNames.Query)]
            public string Query { get; internal set; }

            [JsonProperty(JsonNames.Lenient)]
            public bool Lenient => true;

            [JsonProperty(JsonNames.DefaultOperator)]
            public string DefaultOperator { get; internal set; }

            [JsonProperty(JsonNames.Analyzer)]
            public string Analyzer { get; internal set; }

            [JsonProperty(JsonNames.Fields)]
            public List<string> Fields { get; internal set; }
        }

        [JsonProperty(JsonNames.SimpleQuerystring)]
        public SimpleQueryStringInternal SimpleQueryString { get; private set; }
    }
}