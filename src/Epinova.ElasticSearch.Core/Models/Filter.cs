using System;
using Epinova.ElasticSearch.Core.Enums;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models
{
    internal sealed class Filter
    {
        public Filter(string fieldName, object value, Type type, bool raw, Operator @operator, bool not = false)
        {
            Operator = @operator;
            FieldName = fieldName;
            Value = value;
            Type = type;
            Raw = raw;
            Not = not;
        }


        public string FieldName { get; private set; }

        public object Value { get; private set; }

        public Type Type { get; private set; }

        public bool Raw { get; private set; }

        [JsonIgnore]
        public Operator Operator { get; private set; }

        [JsonIgnore]
        public bool Not { get; private set; }
    }
}