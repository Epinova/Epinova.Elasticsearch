using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class DidYouMeanSuggest
    {
        public DidYouMeanSuggest(string text)
        {
            DidYouMean = new DidYouMeanInternal { Text = text };
        }

        [JsonProperty(JsonNames.DidYouMean)]
        public DidYouMeanInternal DidYouMean { get; set; }


        internal class DidYouMeanInternal
        {
            public DidYouMeanInternal()
            {
                Phrase = new PhraseInternal();
            }

            [JsonProperty(JsonNames.Text)]
            public string Text { get; set; }

            [JsonProperty(JsonNames.Phrase)]
            public PhraseInternal Phrase { get; set; }


            internal class PhraseInternal
            {
                [JsonProperty(JsonNames.Field)]
                public string Field => DefaultFields.DidYouMean;
            }
        }
    }
}