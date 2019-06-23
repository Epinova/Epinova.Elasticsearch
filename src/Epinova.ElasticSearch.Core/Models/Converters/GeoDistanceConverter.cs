using System;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class GeoDistanceConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;

        public override bool CanConvert(Type objectType)
            => typeof(MatchBase).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var geoDistance = value as GeoDistance;

            if(geoDistance?.Point == null)
            {
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.GeoDistance);
            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.Distance);
            writer.WriteValue(geoDistance.Distance);
            writer.WritePropertyName(geoDistance.Field);
            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.Lat);
            writer.WriteValue(geoDistance.Point.Lat);
            writer.WritePropertyName(JsonNames.Lon);
            writer.WriteValue(geoDistance.Point.Lon);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEnd();
        }
    }
}