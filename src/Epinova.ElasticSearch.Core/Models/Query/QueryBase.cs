using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class QueryBase
    {
        [JsonProperty(JsonNames.Bool)]
        public BoolQuery Bool { get; set; } = new BoolQuery();

        public bool ShouldSerializeBool()
        {
            return Bool != null
                   && (Bool.ShouldSerializeFilter()
                       || Bool.ShouldSerializeMust()
                       || Bool.ShouldSerializeMustNot()
                       || Bool.ShouldSerializeShould());
        }
    }
}