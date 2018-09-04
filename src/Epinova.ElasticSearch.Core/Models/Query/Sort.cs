namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Sort
    {
        public bool IsStringField { get; set; }
        public string FieldName { get; set; }
        public string Direction { get; set; }
    }
}