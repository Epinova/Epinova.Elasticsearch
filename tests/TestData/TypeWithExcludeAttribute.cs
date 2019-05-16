using Epinova.ElasticSearch.Core.Attributes;
using EPiServer.Core;

namespace TestData
{
    [ExcludeFromSearch]
    public class TypeWithExcludeAttribute : BasicContent
    {
        public TypeWithExcludeAttribute()
        {
            ContentLink = Factory.GetPageReference();
        }
    }
}
