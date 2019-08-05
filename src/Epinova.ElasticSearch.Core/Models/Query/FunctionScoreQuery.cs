using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class FunctionScoreQuery : Query
    {
        private const string GaussJsonFormat = "\"{0}\": {{ \"scale\": \"{1}\",  \"offset\": \"{2}\" }}";

        public FunctionScoreQuery(Query originalQuery, List<Gauss> gauss)
        {
            Function = new FunctionData(gauss, originalQuery);
            Bool = null;
        }

        public FunctionScoreQuery(Query originalQuery, ScriptScore scriptScore)
        {
            Function = new FunctionData(scriptScore, originalQuery);
            Bool = null;
        }

        [JsonProperty(JsonNames.FunctionScore)]
        public FunctionData Function { get; set; }

        internal class FunctionData
        {
            public FunctionData(List<Gauss> gauss, Query originalQuery)
            {
                Gauss = gauss;
                OriginalQuery = originalQuery;
            }

            public FunctionData(ScriptScore scriptScore, Query originalQuery)
            {
                ScriptScore = scriptScore;
                OriginalQuery = originalQuery;
            }

            [JsonProperty(JsonNames.Query)]
            public Query OriginalQuery { get; set; }

            [JsonIgnore]
            public List<Gauss> Gauss { get; set; }

            [JsonProperty(JsonNames.ScriptScore)]
            public ScriptScore ScriptScore { get; set; }

            [JsonProperty(JsonNames.Gauss)]
            public JObject GaussList
            {
                get
                {
                    if(Gauss?.Any() != true)
                    {
                        return null;
                    }

                    return
                        JObject.Parse("{" + String.Join(",\n",
                            Gauss.Select(g => String.Format(GaussJsonFormat, g.Field, g.Scale, g.Offset))) + "}");
                }
            }
        }
    }
}