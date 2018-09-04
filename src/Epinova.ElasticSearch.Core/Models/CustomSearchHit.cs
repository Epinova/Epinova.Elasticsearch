namespace Epinova.ElasticSearch.Core.Models
{
    public sealed class CustomSearchHit<T>
    {
        public T Item { get; }
        public double Score { get; }
        public string Highlight { get; }

        public CustomSearchHit(T item, double queryScore, string highlight)
        {
            Item = item;
            Score = queryScore;
            Highlight = highlight;
        }
    }
}