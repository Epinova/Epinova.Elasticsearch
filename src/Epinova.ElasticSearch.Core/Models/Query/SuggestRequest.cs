using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class SuggestRequest : RequestBase
    {
        public SuggestRequest(string query, int size, bool includeSkipDuplicates)
        {
            Suggestions = new SuggestionsWrapper
            {
                Suggestions = new Suggestions
                {
                    Text = query,
                    Completion = new Completion
                    {
                        Field = DefaultFields.Suggest,
                        SkipDuplicates = includeSkipDuplicates ? (bool?)true : null,
                        Size = size > 0 ? size : 5
                    }
                }
            };
        }

        [JsonProperty(JsonNames.Source)]
        public bool Source => false;

        [JsonIgnore]
        public override int From { get; internal set; }

        [JsonIgnore]
        public override int Size { get; internal set; }

        public override bool ShouldSerializeFrom() => false;

        public override bool ShouldSerializeSize() => false;

        [JsonProperty(JsonNames.Suggest)]
        internal SuggestionsWrapper Suggestions { get; set; }

        internal class SuggestionsWrapper
        {
            public Suggestions Suggestions { get; set; }
        }
    }
}