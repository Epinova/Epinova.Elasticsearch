using System;

namespace Epinova.ElasticSearch.Core
{
    public static class Constants
    {
        public const string EPiServerConnectionStringName = "EPiServerDB";
        public const string TrackingTable = "ElasticTracking";
        public const string TrackingFieldIndex = "IndexName";
        public const string CommerceProviderName = "CatalogContent";
        public const string IndexEPiServerContentDisplayName = "Elasticsearch: Index CMS content";

        // There was a breaking change in v5.6 renaming the "inline" field to "source" in scripts
        public static Version InlineVsSourceVersion = new Version(5, 6);
    }
}