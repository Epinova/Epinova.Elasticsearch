using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class MoreLikeThisQuery : QueryBase
    {
        [JsonProperty(JsonNames.MoreLikeThis)]
        public MoreLikeThisQueryInternal QueryInternal { get; set; } = new MoreLikeThisQueryInternal();

        internal class MoreLikeThisQueryInternal
        {
            [JsonProperty(JsonNames.Like)]
            public LikeQuery[] Like { get; set; }

            [JsonProperty(JsonNames.MinTermFreq)]
            public int MinTermFreq { get; set; } = 1;

            [JsonProperty(JsonNames.MaxQueryTerms)]
            public int MaxQueryTerms { get; set; } = 12;

            [JsonProperty(JsonNames.MinDocFreq)]
            public int MinDocFreq { get; set; } = 12;

            [JsonProperty(JsonNames.MinWordLength)]
            public int MinWordLength { get; set; } = 12;

            [JsonProperty(JsonNames.Fields)]
            public string[] Fields { get; set; }
        }

        internal class LikeQuery
        {
            [JsonProperty(JsonNames.Id)]
            public string Id { get; set; }
        }
    }
}