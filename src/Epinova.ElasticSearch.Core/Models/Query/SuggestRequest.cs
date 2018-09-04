using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class SuggestRequest : RequestBase
    {
        public SuggestRequest(string query, int size)
        {
            Suggestions = new Suggestions
            {
                Text = query,
                Completion = new Completion
                {
                    Field = DefaultFields.Suggest,
                    Size = size > 0 ? size : 5
                }
            };
        }

        [JsonIgnore]
        public override int From { get; internal set; }

        [JsonIgnore]
        public override int Size { get; internal set; }

        public Suggestions Suggestions { get; set; }
    }
}