using Epinova.ElasticSearch.Core.Enums;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class Sort
    {
        public MappingType MappingType { get; set; }

        public string FieldName { get; set; }

        public string Direction { get; set; }
    }
}