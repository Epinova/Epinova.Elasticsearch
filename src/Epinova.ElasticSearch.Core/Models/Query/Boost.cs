namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal sealed class Boost
    {
        public string FieldName { get; set; }
        public int Weight { get; set; }
    }
}