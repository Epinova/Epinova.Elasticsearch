using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class ScriptScore
    {
        [JsonProperty(JsonNames.Script)]
        public ScriptScoreInner Script { get; set; } = new ScriptScoreInner();


        internal sealed class ScriptScoreInner
        {
            [JsonProperty(JsonNames.Lang)]
            public string Language { get; set; }

            [JsonProperty(JsonNames.ScriptSource)]
            public string Source { get; set; }

            [JsonProperty(JsonNames.Inline)]
            public string Inline { get; set; }

            [JsonProperty(JsonNames.Params)]
            public object Parameters { get; set; }
        }
    }
}