using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Query : QueryBase
    {
        [JsonIgnore]
        public List<string> SearchFields { get; set; } = new List<string>();

        [JsonIgnore]
        public string SearchText { get; set; }

        [JsonProperty(JsonNames.Query)]
        public FunctionScoreQuery FunctionScoreQuery { get; set; }
    }
}