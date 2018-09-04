namespace Epinova.ElasticSearch.Core.Models
{
    public class Tracking
    {
        public string Query { get; set; }
        public long Searches { get; set; }
        public bool NoHits { get; set; }
        public string Language { get; set; }
    }
}