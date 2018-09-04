using Epinova.ElasticSearch.Core.Contracts;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Properties
{
    public class IntegerRange : IProperty
    {
        public IntegerRange(int gte, int lte)
        {
            Gte = gte;
            Lte = lte;
        }


        [JsonProperty(JsonNames.Gte)]
        public int Gte { get; set; }

        [JsonProperty(JsonNames.Lte)]
        public int Lte { get; set; }
    }
}