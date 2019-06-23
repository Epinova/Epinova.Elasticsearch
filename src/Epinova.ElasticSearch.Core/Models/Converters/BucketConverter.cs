using System;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class BucketConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(Bucket);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var bucket = value as Bucket;
            if(bucket == null)
            {
                return;
            }

            writer.WriteStartObject();
            serializer.Serialize(writer, bucket);
            writer.WriteEnd();
        }
    }
}