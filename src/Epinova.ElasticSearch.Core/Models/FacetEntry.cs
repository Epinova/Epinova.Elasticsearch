namespace Epinova.ElasticSearch.Core.Models
{
    public sealed class FacetEntry
    {
        public FacetEntry()
        {
            Hits = new FacetHit[0];
        }

        public string Key { get; set; }

        public int Count { get; set; }

        public FacetHit[] Hits { get; set; }
    }
}