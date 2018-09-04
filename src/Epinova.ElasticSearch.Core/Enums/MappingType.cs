// ReSharper disable InconsistentNaming
namespace Epinova.ElasticSearch.Core.Enums
{
    /// <summary>
    /// https://www.elastic.co/guide/en/elasticsearch/reference/1.7/mapping-core-types.html
    /// string, integer/long, float/double, boolean, date
    /// </summary>
    public enum MappingType
    {
        String = 0,
        Text,
        Attachment,
        Integer,
        Long,
        Float,
        Double,
        Boolean,
        Date,
        Integer_Range
    }
}