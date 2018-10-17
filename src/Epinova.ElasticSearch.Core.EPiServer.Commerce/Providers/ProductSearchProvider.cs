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
        private static Injected<DefaultContentProvider> DefaultContentProvider { get; set; }
        private static Injected<ReferenceConverter> ReferenceConverter { get; set; }

        public ProductSearchProvider() : base("product")
        {
            IconClass = Constants.CommerceCatalogIconCssClass;
            AreaName = Constants.CommerceCatalogArea;
            ForceRootLookup = true;
            IndexName = GetIndexName();
        }

        private string GetIndexName()
        {
            return $"{_elasticSearchSettings.Index}-{Core.Constants.CommerceProviderName}-{Language.GetLanguageCode(GetLanguage())}";
        }

        protected override string GetSearchRoot()
        {
            return ReferenceConverter.Service.GetRootLink().ID.ToString();
        }

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