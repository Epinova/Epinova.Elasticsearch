﻿using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models
{
    internal abstract class RequestBase
    {
        //TODO: Should this be virtual?
        [JsonProperty(JsonNames.From)]
        public abstract int From { get; internal set; }

        //TODO: Should this be virtual?
        [JsonProperty(JsonNames.Size)]
        public abstract int Size { get; internal set; }

        [JsonIgnore]
        public bool IsPartOfFilteredQuery { get; set; }


        public bool ShouldSerializeFilter()
        {
            return IsPartOfFilteredQuery;
        }


        public bool ShouldSerializeFrom()
        {
            return IsPartOfFilteredQuery == false && GetType() != typeof(SuggestRequest);
        }


        public bool ShouldSerializeSize()
        {
            return IsPartOfFilteredQuery == false && GetType() != typeof(SuggestRequest);
        }


        public string ToString(Formatting formatting)
        {
            return JsonConvert.SerializeObject(
                this,
                formatting,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        }


        public override string ToString()
        {
            return ToString(Formatting.None);
        }
    }
}