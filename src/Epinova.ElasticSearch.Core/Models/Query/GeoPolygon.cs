using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models.Converters;
using Epinova.ElasticSearch.Core.Models.Properties;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    [JsonConverter(typeof(GeoPolygonConverter))]
    internal class GeoPolygon : MatchBase
    {
        public GeoPolygon(string field, IEnumerable<GeoPoint> points)
        {
            Field = field;
            Points = points;
        }

        [JsonIgnore]
        public string Field { get; set; }

        [JsonIgnore]
        public IEnumerable<GeoPoint> Points { get; set; }
    }
}