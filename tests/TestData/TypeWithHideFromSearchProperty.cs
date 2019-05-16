using Epinova.ElasticSearch.Core.Attributes;
using EPiServer.Core;

namespace TestData
{
    [ExcludeFromSearch]
    public sealed class TypeWithHideFromSearchProperty : BasicContent
    {
        public TypeWithHideFromSearchProperty()
        {
            Property["HideFromSearch"] = new PropertyBoolean(true);
            ContentLink = Factory.GetPageReference();
        }
    }
}
