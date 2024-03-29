﻿using System;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public class BulkMetadataBase
    {
        [JsonProperty(JsonNames.Index)]
        public string Index { get; set; }

        [JsonIgnore]
        public string IndexCandidate { get; set; }

        [JsonProperty(JsonNames.Error)]
        public Error Error { get; set; }

        [JsonIgnore]
        public Type DataType { get; set; }

        [JsonProperty(JsonNames.Id)]
        public long Id { get; set; }
    }
}