using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class NestedBoolQuery : MatchBase
    {
        public NestedBoolQuery(BoolQuery inner)
        {
            Bool = inner;
        }


        [JsonProperty(JsonNames.Bool)]
        public BoolQuery Bool { get; set; }


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