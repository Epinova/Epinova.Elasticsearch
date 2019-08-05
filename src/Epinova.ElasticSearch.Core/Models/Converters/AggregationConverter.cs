using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class AggregationConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(Dictionary<string, Bucket>);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var buckets = value as Dictionary<string, Bucket>;
            if(buckets == null)
            {
                return;
            }

            writer.WriteStartObject();

            foreach(KeyValuePair<string, Bucket> bucket in buckets)
            {
                string name = bucket.Value.Terms.Field.Replace(Constants.KeywordSuffix, String.Empty);

                writer.WritePropertyName(name);
                serializer.Serialize(writer, bucket.Value);
            }

            writer.WriteEnd();
        }
    }
}