using System.Linq;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class MoreLikeThisRequest : RequestBase
    {
        public MoreLikeThisRequest(QuerySetup querySetup)
        {
            From = querySetup.From;
            Size = querySetup.Size;

            Query.QueryInternal.MinTermFreq = querySetup.MltMinTermFreq;
            Query.QueryInternal.MinDocFreq = querySetup.MltMinDocFreq;
            Query.QueryInternal.MinWordLength = querySetup.MltMinWordLength;
            Query.QueryInternal.MaxQueryTerms = querySetup.MltMaxQueryTerms;
            Query.QueryInternal.Fields = querySetup.SearchFields.ToArray();

            if(Query.QueryInternal.Fields.Length == 0)
            {
                Query.QueryInternal.Fields = WellKnownProperties.AutoAnalyze
                    .Concat(new[] { DefaultFields.Name })
                    .Except(new[] { DefaultFields.AttachmentContent })
                    .ToArray();
            }

            Query.QueryInternal.Like = new MoreLikeThisQuery.LikeQuery[]
            {
                new MoreLikeThisQuery.LikeQuery
                {
                    Id = querySetup.MoreLikeId
                }
            };
        }

        [JsonProperty(JsonNames.Query)]
        public MoreLikeThisQuery Query { get; internal set; } = new MoreLikeThisQuery();

        [JsonProperty(JsonNames.From)]
        public override int From { get; internal set; }

        [JsonProperty(JsonNames.Size)]
        public override int Size { get; internal set; }
    }
}
