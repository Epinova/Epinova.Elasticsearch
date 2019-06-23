using System;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models
{
    internal sealed class Facet
    {
        [JsonProperty(JsonNames.Key)]
        private string KeyDefault { get; set; }

        [JsonProperty(JsonNames.KeyAsString)]
        private string KeyAsString { get; set; }

        public string Key
        {
            get
            {
                if(!String.IsNullOrWhiteSpace(KeyAsString))
                {
                    return KeyAsString;
                }

                return KeyDefault;
            }
        }

        [JsonProperty(JsonNames.DocCount)]
        public int Count { get; set; }
    }
}