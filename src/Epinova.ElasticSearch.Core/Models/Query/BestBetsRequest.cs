using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class BestBetsRequest
    {
        public BestBetsRequest(string[] bestbets)
        {
            Document = new Doc
            {
                BestBets = bestbets
            };
        }

        [JsonProperty(JsonNames.Doc)]
        public Doc Document { get; private set; }


        internal class Doc
        {
            [JsonProperty(DefaultFields.BestBets)]
            public string[] BestBets { get; set; }
        }
    }
}