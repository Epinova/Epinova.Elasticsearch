namespace Epinova.ElasticSearch.Core.EPiServer.Providers
{
    // Borrowed from ContentSearchProviderConstants

    public static class ProviderConstants
    {
        // Properties
        public static string BlockArea => "CMS/blocks";

        public static string BlockCategory => "/shell/cms/search/blocks/category";

        public static string BlockIconCssClass => "epi-resourceIcon epi-resourceIcon-block";

        public static string BlockToolTipContentTypeNameResourceKey => "blocktype";

        public static string BlockToolTipResourceKeyBase => "/shell/cms/search/blocks/tooltip";

        public static string FileArea => "CMS/files";

        public static string FileCategory => "/shell/cms/search/files/category";

        public static string FileIconCssClass => "epi-resourceIcon epi-resourceIcon-file";

        public static string FileToolTipContentTypeNameResourceKey => "filetype";

        public static string FileToolTipResourceKeyBase => "/shell/cms/search/files/tooltip";

        public static string PageArea => "CMS/pages";

        public static string PageCategory => "/shell/cms/search/pages/category";

        public static string PageIconCssClass => "epi-resourceIcon epi-resourceIcon-page";

        public static string PageToolTipContentTypeNameResourceKey => "pagetype";

        public static string PageToolTipResourceKeyBase => "/shell/cms/search/pages/tooltip";

        public const string CatalogProviderKey = "CatalogContent";

        public const string CmsProviderKey = "cms";

        public const string DefaultProviderKey = "default";

        public static string CommerceCatalogArea => "Commerce/Catalog";

        public static string CommerceCatalogIconCssClass => "epi-resourceIcon epi-resourceIcon-page";

        public static string CommerceCampaignsArea => "Commerce/Campaigns";

        public static string CommerceCampaignsIconCssClass => "epi-resourceIcon";
    }
}