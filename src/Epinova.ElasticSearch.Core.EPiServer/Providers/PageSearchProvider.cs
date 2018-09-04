using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Shell.Search;

namespace Epinova.ElasticSearch.Core.EPiServer.Providers
{
    [SearchProvider]
    public class PageSearchProvider : SearchProviderBase<PageData, PageData, PageType>
    {
        public PageSearchProvider() : base("page")
        {
            IconClass = ProviderConstants.PageIconCssClass;
            AreaName = ProviderConstants.PageArea;
        }
    }
}