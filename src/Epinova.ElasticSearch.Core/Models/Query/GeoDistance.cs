using Epinova.ElasticSearch.Core.Models.Converters;
using Epinova.ElasticSearch.Core.Models.Properties;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    [JsonConverter(typeof(GeoDistanceConverter))]
    internal class GeoDistance : MatchBase
    {
        public GeoDistance(string field, string distance, GeoPoint center)
        {
            Field = field;
            Distance = distance;
            Point = center;
        }

        public string Field { get; set; }

        public string Distance { get; set; }

        public GeoPoint Point { get; set; }
    }
}