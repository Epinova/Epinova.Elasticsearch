using System;
using System.Linq;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class RangeConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;


        public override bool CanConvert(Type objectType)
            => typeof(MatchBase).IsAssignableFrom(objectType);


        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var parameters = value.GetType()
                .GetProperties()
                .Select(p => new
                {
                    p.Name,
                    Value = p.GetValue(value),
                    Type = p.PropertyType,
                    Inclusive = p.GetCustomAttributes(false).Any(a => a is InclusiveAttribute)
                }).ToArray();

            string fieldProperty = "Field";
            string relationProperty = "Relation";
            string inclusiveProperty = "Inclusive";
            string fieldName = (string)parameters.First(p => p.Name == fieldProperty).Value;
            bool inclusive = (bool)parameters.First(p => p.Name == inclusiveProperty).Value;
            var properties = parameters
                .Where(p => p.Name != fieldProperty && p.Name != inclusiveProperty)
                .Where(p => p.Inclusive == inclusive || p.Name == relationProperty)
                .Where(p => p.Value != null);

            writer.WriteStartObject();
            writer.WritePropertyName(fieldName);

            writer.WriteStartObject();
            foreach(var property in properties)
            {
                var type = property.Type.IsGenericType
                    ? property.Type.GenericTypeArguments[0]
                    : property.Type;

                writer.WritePropertyName(property.Name.ToLower());
                writer.WriteValue(Convert.ChangeType(property.Value, type));
            }
            writer.WriteEndObject();

            writer.WriteEnd();
        }
    }
}