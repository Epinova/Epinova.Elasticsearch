using System;
using System.Linq;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class BulkResultItemConverter : JsonConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            string operationRaw = jo.Properties().First().Name;

            BulkMetadataBase metadata = jo[operationRaw].ToObject<BulkMetadataBase>();
            BulkResultItemStatus status = jo[operationRaw].ToObject<BulkResultItemStatus>();

            var item = new BulkResultItem();
            item.Populate(metadata, status);

            Enum.TryParse(operationRaw, true, out Operation operation);
            item.Operation = operation;

            return item;
        }

        public override bool CanConvert(Type objectType)
            => objectType == typeof(BulkResultItem);

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();
    }
}