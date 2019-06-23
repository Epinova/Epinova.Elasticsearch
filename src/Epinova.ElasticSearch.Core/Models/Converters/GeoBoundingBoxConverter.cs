using System;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class GeoBoundingBoxConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;

        public override bool CanConvert(Type objectType)
            => typeof(MatchBase).IsAssignableFrom(objectType);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var geoBox = value as GeoBoundingBox;

            if(geoBox?.Box == null)
            {
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.GeoBoundingBox);

            writer.WriteStartObject();
            writer.WritePropertyName(geoBox.Field);

            writer.WriteStartObject();

            writer.WritePropertyName(JsonNames.TopLeft);
            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.Lat);
            writer.WriteValue(geoBox.Box.TopLeft.Lat);
            writer.WritePropertyName(JsonNames.Lon);
            writer.WriteValue(geoBox.Box.TopLeft.Lon);
            writer.WriteEndObject();

            writer.WritePropertyName(JsonNames.BottomRight);
            writer.WriteStartObject();
            writer.WritePropertyName(JsonNames.Lat);
            writer.WriteValue(geoBox.Box.BottomRight.Lat);
            writer.WritePropertyName(JsonNames.Lon);
            writer.WriteValue(geoBox.Box.BottomRight.Lon);
            writer.WriteEndObject();

            writer.WriteEndObject(); // End geoBox.Field
            writer.WriteEndObject(); // End JsonNames.GeoBoundingBox
            writer.WriteEnd();
        }
    }
}