using Epinova.ElasticSearch.Core.EPiServer.Providers;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Search;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Providers
{
    [SearchProvider]
    public class CampaignSearchProvider : SearchProviderBase<IContent, IContent, ContentType>
    {
#pragma warning disable RCS1170 // Use read-only auto-implemented property.
        private static Injected<ContentRootService> RootService { get; set; }
        private static Injected<DefaultContentProvider> DefaultContentProvider { get; set; }
#pragma warning restore RCS1170 // Use read-only auto-implemented property.

        public CampaignSearchProvider() : base("campaigns")
        {
            IconClass = ProviderConstants.CommerceCampaignsIconCssClass;
            AreaName = ProviderConstants.CommerceCampaignsArea;
            ForceRootLookup = true;
        }

        protected override string GetSearchRoot()
            => RootService.Service.Get("SysCampaignRoot").ID.ToString();

        protected override string[] GetProviderKeys()
        {
            return new[]
            {
                ProviderConstants.CatalogProviderKey,
                DefaultContentProvider.Service.ProviderKey ?? ProviderConstants.DefaultProviderKey
            };
        }
    }
}