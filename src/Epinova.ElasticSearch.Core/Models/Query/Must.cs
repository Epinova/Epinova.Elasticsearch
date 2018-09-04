using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class Must
    {
        public class MustInternal
        {
            [JsonProperty(JsonNames.MultiMatch)]
            public MatchMulti MultiMatch { get; set; }

            public Match Match { get; set; }

            public string Term { get; set; }
        }

        public Must(Match match, string term)
        {
            MustBody = new MustInternal
            {
                Match = match,
                Term = term
            };
        }

        [JsonProperty(JsonNames.Must)]
        public MustInternal MustBody { get; private set; }
    }
}