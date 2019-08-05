using System;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class GeoPolygonConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;

        public override bool CanConvert(Type objectType)
            => typeof(MatchBase).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var geoPoly = value as GeoPolygon;

            if(geoPoly?.Points == null)
            {
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.GeoPolygon);
            writer.WriteStartObject();
            writer.WritePropertyName(geoPoly.Field);
            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.Points);
            writer.WriteStartArray();
            foreach(var point in geoPoly.Points)
            {
                writer.WriteStartObject();
                writer.WritePropertyName(JsonNames.Lat);
                writer.WriteValue(point.Lat);
                writer.WritePropertyName(JsonNames.Lon);
                writer.WriteValue(point.Lon);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEnd();
        }
    }
}