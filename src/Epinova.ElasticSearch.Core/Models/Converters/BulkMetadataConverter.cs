using System;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class BulkMetadataConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(BulkMetadata);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            BulkMetadata bulk = value as BulkMetadata;
            if(bulk == null)
            {
                return;
            }

            string name = Enum.GetName(typeof(Operation), bulk.Operation);
            if(name == null)
            {
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(name.ToLower());
            serializer.Converters.Remove(this);
            serializer.Serialize(writer, bulk);
            serializer.Converters.Add(this);
            writer.WriteEndObject();
        }
    }
}