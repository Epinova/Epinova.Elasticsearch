using System;
using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Enums;
using EPiServer.Logging;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Mapping
{
    internal class IndexMapping
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(IndexMapping));

        public IndexMapping()
        {
            Properties = new Dictionary<string, IndexMappingProperty>();
        }

        [JsonIgnore]
        public bool IsDirty { get; private set; }

        [JsonProperty(JsonNames.Properties)]
        public Dictionary<string, IndexMappingProperty> Properties { get; set; }

        public void AddOrUpdateProperty(string name, IndexMappingProperty property)
        {
            Logger.Debug("Property: " + name);

            if(property == null)
            {
                property = new IndexMappingProperty
                {
                    Type = nameof(MappingType.Text).ToLower()
                };
            }

            if(!Properties.ContainsKey(name))
            {
                Logger.Debug("Adding property");
                Properties.Add(name, property);
                IsDirty = true;
            }

            if(property.Analyzer != null)
            {
                Logger.Debug("Analyzer: " + property.Analyzer);

                if(Properties[name].Analyzer != property.Analyzer)
                {
                    IsDirty = true;
                }

                Properties[name].Analyzer = property.Analyzer;
            }

            if(property.Format != null)
            {
                Logger.Debug("Format: " + property.Format);

                if(Properties[name].Format != property.Format)
                {
                    IsDirty = true;
                }

                Properties[name].Format = property.Format;
            }

            if(property.Type != null)
            {
                Logger.Debug("Type: " + property.Type);

                if(Properties[name].Type != property.Type)
                {
                    IsDirty = true;
                }

                if(property.Type == nameof(MappingType.Text).ToLower())
                {
                    Logger.Debug("Type is string");
                    property.Fields = property.Fields ?? new IndexMappingProperty.ContentProperty();
                    property.Fields.KeywordSettings = new IndexMappingProperty.ContentProperty.Keyword
                    {
                        IgnoreAbove = 256,
                        Type = JsonNames.Keyword
                    };
                }
                else if(property.Type == nameof(MappingType.Object).ToLower())
                {
                    Logger.Debug("Type is Object");
                    property.Dynamic = true;
                    property.Properties = new Dictionary<string, object>();
                }

                Properties[name].Type = property.Type;
            }

            if(Properties[name].CopyTo != null && !Properties[name].CopyTo.SequenceEqual(property.CopyTo))
            {
                IsDirty = true;
                Properties[name].CopyTo = property.CopyTo;
            }

            if(Properties[name].CopyTo != null)
            {
                Logger.Debug("CopyTo: " + String.Join(", ", Properties[name].CopyTo));
            }
        }
    }
}