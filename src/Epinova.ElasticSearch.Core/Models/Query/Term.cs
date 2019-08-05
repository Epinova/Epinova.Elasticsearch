using System;
using Epinova.ElasticSearch.Core.Models.Converters;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Term : MatchBase
    {
        public Term(string key, object value, bool nonRaw = false, Type dataType = null)
        {
            TermItem = new TermItem
            {
                Key = key,
                Value = value,
                NonRaw = nonRaw,
                Type = dataType
            };
        }

        [JsonProperty(JsonNames.Term)]
        public TermItem TermItem { get; set; }

        public static Terms FromArrayFilter(Filter filter)
            => new Terms(filter.FieldName, filter.Value, !filter.Raw, filter.Type);

        public static Terms FromArrayFilter(Filter filter, object valueOverride)
            => new Terms(filter.FieldName, valueOverride, !filter.Raw, filter.Type);

        public static Term FromFilter(Filter filter)
            => new Term(filter.FieldName, filter.Value, !filter.Raw, filter.Type);

        public static Term FromFilter(Filter filter, object valueOverride)
            => new Term(filter.FieldName, valueOverride, !filter.Raw, filter.Type);
    }

    [JsonConverter(typeof(TermConverter))]
    internal class TermItem : MatchBase
    {
        [JsonIgnore]
        public bool NonRaw { get; set; }

        [JsonIgnore]
        public string Key { get; set; }

        [JsonIgnore]
        public Type Type { get; set; }

        [JsonIgnore]
        public object Value { get; set; }
    }
}