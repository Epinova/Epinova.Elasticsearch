using System;
using Epinova.ElasticSearch.Core.Models.Properties;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class GeoPointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(GeoPoint);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => GeoPoint.Parse(reader.Value as string);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var geoPoint = value as GeoPoint;

            if(geoPoint == null)
            {
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.Lat);
            writer.WriteValue(geoPoint.Lat);
            writer.WritePropertyName(JsonNames.Lon);
            writer.WriteValue(geoPoint.Lon);
            writer.WriteEnd();
        }
    }
}