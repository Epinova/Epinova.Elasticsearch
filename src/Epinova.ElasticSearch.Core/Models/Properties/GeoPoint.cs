using System;
using System.Globalization;
using Epinova.ElasticSearch.Core.Contracts;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Properties
{
    /// <summary>
    /// Type representing a geopoint, with latitude and longitude. 
    /// </summary>
    public class GeoPoint : IProperty
    {
        public GeoPoint(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
        }

        public GeoPoint()
        {
        }

        [JsonProperty(JsonNames.Lat)]
        public double Lat { get; set; }

        [JsonProperty(JsonNames.Lng)]
        public double Lng { get; set; }

        public override string ToString() => $"{Lat},{Lng}";

        /// <summary>
        /// Parse a lat-lng string as a <see cref="GeoPoint" />. 
        /// </summary>
        /// <param name="latlng">The coordinates expressed as "lat,lng"</param>
        /// <returns>A new instance of <see cref="GeoPoint"/> or null in case of invalid input</returns>
        public static GeoPoint Parse(string latlng)
        {
            if (latlng == null || latlng.IndexOf(",") < 1)
            {
                return null;
            }

            var segments = latlng.Split(',');

            if (segments.Length != 2
                || !Double.TryParse(segments[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat)
                || !Double.TryParse(segments[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng))
            {
                return null;
            }

            if(lat > 90 || lat < -90 || lng > 180 || lng < -180)
            {
                return null;
            }

            return new GeoPoint(lat, lng);
        }
    }
}