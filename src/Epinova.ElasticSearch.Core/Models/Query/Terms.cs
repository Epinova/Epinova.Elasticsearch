using System;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Terms : Term
    {
        public Terms(string key, object value, bool nonRaw = false, Type dataType = null)
            : base(key, value, nonRaw, dataType)
        {
            TermsItem = new TermItem
            {
                Key = key,
                Value = value,
                NonRaw = nonRaw,
                Type = dataType
            };

            TermItem = null;
        }

        [JsonProperty(JsonNames.Terms)]
        public TermItem TermsItem { get; set; }
    }
}