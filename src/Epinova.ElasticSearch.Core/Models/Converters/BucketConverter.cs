using System;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{

    public class BucketConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return null;
        }

        

        public override bool CanConvert(Type objectType)
        {
            return objectType  == typeof(Bucket);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Bucket bucket = value as Bucket;
            if (bucket == null)
                return;

            writer.WriteStartObject();
            writer.WritePropertyName(bucket.Terms.Field.Replace(Constants.RawSuffix, String.Empty));
            serializer.Serialize(writer, bucket);
            writer.WriteEnd();
        }
    }
}