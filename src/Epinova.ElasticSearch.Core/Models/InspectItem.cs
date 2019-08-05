using System;
using Epinova.ElasticSearch.Core.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Models
{
    public class InspectItem
    {
        public string Title { get; }

        public string ShortTypeName { get; }

        public string Content { get; }

        public InspectItem(JToken token)
        {
            JObject instance = token.Value<JObject>().Property("_source").Value as JObject;

            if(instance == null)
            {
                return;
            }

            Title = GetTitle(instance);
            string type = instance.Property("Type")?.Value.ToString();
            ShortTypeName = type.GetShortTypeName();
            Content = instance.ToString(Formatting.Indented);
        }

        private string GetTitle(JObject instance)
        {
            string propertyValue = GetPropertyValue(instance, "Name");
            if(!String.IsNullOrWhiteSpace(propertyValue))
            {
                return propertyValue;
            }

            propertyValue = GetPropertyValue(instance, "Title");
            if(!String.IsNullOrWhiteSpace(propertyValue))
            {
                return propertyValue;
            }

            return "Name or Title should be indexed.";
        }

        private string GetPropertyValue(JObject instance, string propertyName)
        {
            JProperty property = instance.Property(propertyName);
            return property?.Value.ToString();
        }
    }
}