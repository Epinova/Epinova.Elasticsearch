using System.Collections.Generic;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class BoolQuery
    {
        [JsonProperty(JsonNames.Must)]
        public List<MatchBase> Must { get; set; } = new List<MatchBase>();

        [JsonProperty(JsonNames.MustNot)]
        public List<MatchBase> MustNot { get; set; } = new List<MatchBase>();

        [JsonProperty(JsonNames.Should)]
        public List<MatchBase> Should { get; set; } = new List<MatchBase>();

        [JsonIgnore]
        public int? MinimumNumberShouldMatch { get; set; }

        [JsonProperty(JsonNames.Filter)]
        public List<MatchBase> Filter { get; set; } = new List<MatchBase>();


        public bool ShouldSerializeMust()
        {
            return Must != null && Must.Count > 0;
        }

        public bool ShouldSerializeMustNot()
        {
            return MustNot != null && MustNot.Count > 0;
        }

        public bool ShouldSerializeShould()
        {
            return Should != null && Should.Count > 0;
        }

        public bool ShouldSerializeFilter()
        {
            return Filter != null && Filter.Count > 0;
        }
    }
}