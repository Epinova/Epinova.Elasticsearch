namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class Gauss
    {
        public string Field { get; set; }
        public string Scale { get; set; }
        public string Offset { get; set; }
    }
}