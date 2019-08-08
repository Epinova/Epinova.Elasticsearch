using System;
using Epinova.ElasticSearch.Core.Models.Properties;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class GeoPointConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => GeoPoint.Parse(reader.Value as string);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();
    }
}