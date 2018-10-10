using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce
{
    public static class Constants
    {
        /// <summary>
        /// ReferenceConverter.GetRootLink() is replaced with hardcoded reference to avoid Commerce dependency. 
        /// It's a constant value in Commerce, so it will most likely never change.
        /// </summary>
        public static ContentReference CatalogRootLink = new ContentReference(-1073741823, 0, Core.Constants.CommerceProviderName);

        public static string CommerceCatalogArea => "Commerce/Catalog";

        public static string CommerceCatalogIconCssClass => "epi-resourceIcon epi-resourceIcon-page";

        public static string CommerceCampaignsArea => "Commerce/Campaigns";

        public static string CommerceCampaignsIconCssClass => "epi-resourceIcon";
    }
}