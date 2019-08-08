using System;
using System.Globalization;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models.Converters;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Properties
{
    /// <summary>
    /// Type representing a geopoint, with latitude and longitude. 
    /// </summary>
    [JsonConverter(typeof(GeoPointConverter))]
    public class GeoPoint : IProperty
    {
        public GeoPoint(double lat, double lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public GeoPoint()
        {
        }

        [JsonProperty(JsonNames.Lat)]
        public double Lat { get; set; }

        [JsonProperty(JsonNames.Lon)]
        public double Lon { get; set; }

        public override string ToString() => FormattableString.Invariant($"{Lat},{Lon}");

        /// <summary>
        /// Parse a lat-lon string as a <see cref="GeoPoint" />. 
        /// </summary>
        /// <param name="latlon">The coordinates expressed as "lat,lon"</param>
        /// <returns>A new instance of <see cref="GeoPoint"/> or null in case of invalid input</returns>
        public static GeoPoint Parse(string latlon)
        {
            if(latlon == null || latlon.IndexOf(",") < 1)
            {
                return null;
            }

            var segments = latlon.Split(',');

            if(segments.Length != 2
                || !Double.TryParse(segments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !Double.TryParse(segments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                return null;
            }

            if(lat > 90 || lat < -90 || lon > 180 || lon < -180)
            {
                return null;
            }

            return new GeoPoint(lat, lon);
        }
    }
}