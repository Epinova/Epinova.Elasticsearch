using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class QueryRequest : RequestBase
    {
        private readonly List<Sort> _sortFields;

        public QueryRequest(QuerySetup querySetup)
        {
            _sortFields = querySetup.SortFields;
            From = querySetup.From;
            Size = querySetup.Size;
            Operator = querySetup.Operator;
        }

        [JsonProperty(JsonNames.Sort)]
        public JArray Sort
        {
            get
            {
                if (_sortFields == null || _sortFields.Count == 0)
                    return null;

                var sorts = new JArray();

                for (var i = 0; i < _sortFields.Count; i++)
                {
                    string sortField = _sortFields[i].FieldName;

                    if (_sortFields[i].MappingType == MappingType.Geo_Point && _sortFields[i] is GeoSort geoSort)
                    {
                        sorts.Add(
                            new JObject(
                                new JProperty("_geo_distance",
                                    new JObject(
                                        new JProperty(sortField, new[] { geoSort.CompareTo.Lat, geoSort.CompareTo.Lon }),
                                        new JProperty(JsonNames.Order, geoSort.Direction),
                                        new JProperty(JsonNames.Unit, geoSort.Unit),
                                        new JProperty(JsonNames.Mode, geoSort.Mode)
                            ))));
                    }
                    else
                    {
                        if (sortField[0] != '_' && _sortFields[i].MappingType == MappingType.Text)
                            sortField += Constants.KeywordSuffix;

                        sorts.Add(
                            new JObject(
                                new JProperty(sortField,
                                    new JObject(new JProperty(JsonNames.Order, _sortFields[i].Direction))))
                            );
                    }
                }

                return sorts;
            }
        }

        [JsonProperty(JsonNames.Source)]
        public string[] SourceFields { get; internal set; }

        [JsonIgnore]
        private Operator Operator { get; set; }

        [JsonProperty(JsonNames.From)]
        public override int From { get; internal set; }

        [JsonProperty(JsonNames.Size)]
        public override int Size { get; internal set; }

        [JsonProperty(JsonNames.Aggregations)]
        [JsonConverter(typeof(AggregationConverter))]
        public Dictionary<string, Bucket> Aggregation { get; set; }

        [JsonProperty(JsonNames.Query)]
        public Query Query { get; internal set; } = new Query();

        [JsonProperty(JsonNames.Suggest)]
        public DidYouMeanSuggest DidYouMeanSuggest { get; internal set; }

        [JsonProperty(JsonNames.Highlight)]
        public Highlight Highlight { get; set; }

        [JsonProperty(JsonNames.PostFilter)]
        public PostFilter PostFilter { get; set; } = new PostFilter();

        public bool ShouldSerializePostFilter()
        {
            return PostFilter?.ShouldSerializeBool() == true;
        }

        public bool ShouldSerializeAggregation()
        {
            return !IsPartOfFilteredQuery;
        }
    }
}
