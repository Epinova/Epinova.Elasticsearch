using Epinova.ElasticSearch.Core.Attributes;

namespace TestData
{
    public class TypeWithBoosting
    {
        [Boost]
        public string BoostedProperty { get; set; }
    }
}
