using Epinova.ElasticSearch.Core.EPiServer.Providers;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Search;
using Mediachase.Commerce.Catalog;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Providers
{
    [SearchProvider]
    public class ProductSearchProvider : SearchProviderBase<IContent, IContent, ContentType>
    {
#pragma warning disable RCS1170 // Use read-only auto-implemented property.
        private static Injected<DefaultContentProvider> DefaultContentProvider { get; set; }
        private static Injected<ReferenceConverter> ReferenceConverter { get; set; }
#pragma warning restore RCS1170 // Use read-only auto-implemented property.

        public ProductSearchProvider() : base("product")
        {
            IconClass = ProviderConstants.CommerceCatalogIconCssClass;
            AreaName = ProviderConstants.CommerceCatalogArea;
            ForceRootLookup = true;
            IndexName = GetIndexName();
        }

        private string GetIndexName()
            => $"{_elasticSearchSettings.Index}-{Constants.CommerceProviderName}-{Language.GetRequestLanguageCode()}";

        protected override string GetSearchRoot() => ReferenceConverter.Service.GetRootLink().ID.ToString();

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