using Epinova.ElasticSearch.Core.Models.Converters;
using Epinova.ElasticSearch.Core.Models.Properties;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    [JsonConverter(typeof(GeoBoundingBoxConverter))]
    internal class GeoBoundingBox : MatchBase
    {
        public GeoBoundingBox(string field, GeoPoint topLeft, GeoPoint bottomRight)
        {
            Field = field;
            Box = new BoundingBox
            {
                TopLeft = topLeft,
                BottomRight = bottomRight
            };
        }

        [JsonIgnore]
        public string Field { get; set; }

        [JsonProperty(JsonNames.GeoBoundingBox)]
        public BoundingBox Box { get; }

        internal class BoundingBox
        {
            [JsonProperty(JsonNames.TopLeft)]
            public GeoPoint TopLeft { get; set; }

            [JsonProperty(JsonNames.BottomRight)]
            public GeoPoint BottomRight { get; set; }
        }
    }
}