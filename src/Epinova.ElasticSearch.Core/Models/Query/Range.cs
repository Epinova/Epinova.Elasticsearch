using System;
using Epinova.ElasticSearch.Core.Models.Converters;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Range<T> : RangeBase
        where T : struct
    {
        public Range(string field, bool inclusive)
        {
            RangeSetting = new RangeItem
            {
                Inclusive = inclusive,
                Field = field
            };
        }

        [JsonProperty(JsonNames.Range)]
        public RangeItem RangeSetting { get; private set; }

        [JsonConverter(typeof(RangeConverter))]
        internal class RangeItem : RangeItemBase<T>
        {
            [JsonProperty(JsonNames.Relation)]
            public string Relation { get; set; }
        }
    }

    internal class InclusiveAttribute : Attribute
    {
    }

    internal class RangeItemBase<T> : MatchBase where T : struct
    {
        public RangeItemBase(string field, T gt, T lt, T gte, T lte)
        {
            Field = field;
            Gt = gt;
            Lt = lt;
            Gte = gte;
            Lte = lte;
        }

        public RangeItemBase()
        {
        }


        [JsonIgnore]
        public bool Inclusive { get; set; }

        [JsonIgnore]
        public string Field { get; set; }

        [JsonIgnore]
        public T Gt { get; set; }

        [JsonIgnore]
        [Inclusive]
        public T Gte { get; set; }

        [JsonIgnore]
        public T? Lt { get; set; }

        [JsonIgnore]
        [Inclusive]
        public T? Lte { get; set; }
    }
}