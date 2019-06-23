using System;
using Epinova.ElasticSearch.Core.Models.Query;
using Epinova.ElasticSearch.Core.Utilities;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Converters
{
    public class TermConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => null;

        public override bool CanConvert(Type objectType)
            => objectType == typeof(Term);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var term = value as TermItem;
            if(term == null)
            {
                return;
            }

            string name = term.Key;
            if(!term.NonRaw && term.Type != null && term.Type == typeof(string))
            {
                name += Constants.KeywordSuffix;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(name);

            if(ArrayHelper.IsArrayCandidate(term.Value.GetType()))
            {
                writer.WriteStartArray();
                foreach(object item in ArrayHelper.ToArray(term.Value) as object[] ?? new object[0])
                {
                    writer.WriteValue(item);
                }
                writer.WriteEndArray();
            }
            else
            {
                writer.WriteValue(term.Value);
            }

            writer.WriteEnd();
        }
    }
}