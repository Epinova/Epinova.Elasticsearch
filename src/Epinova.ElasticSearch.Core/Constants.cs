using System;

namespace Epinova.ElasticSearch.Core
{
    public static class Constants
    {
        public const string EPiServerConnectionStringName = "EPiServerDB";
        public const string TrackingFieldIndex = "IndexName";
        public const string CommerceProviderName = "CatalogContent";
        public const string IndexEPiServerContentDisplayName = "Elasticsearch: Index CMS content";

        internal static readonly Version MinimumSupportedVersion = new Version(5, 0);

        // There was a breaking change in v5.6 renaming the "inline" field to "source" in scripts
        internal static readonly Version InlineVsSourceVersion = new Version(5, 6);

        // Flag "skip_duplicates" was added in v6.1 for suggestions
        internal static readonly Version SkipDuplicatesFieldVersion = new Version(6, 1);

        // Param "rest_total_hits_as_int" was added in v7.0
        internal static readonly Version TotalHitsAsIntAddedVersion = new Version(7, 0);

        // Param "include_type_name" was added in v7.0
        internal static readonly Version IncludeTypeNameAddedVersion = new Version(7, 0);

        internal static class Tracking
        {
            internal const string TableName = "ElasticTracking";

            internal static class Sql
            {
                internal const string Definition = "[Query] [nvarchar](400) NOT NULL, "
                    + "[Searches] [int] NOT NULL, "
                    + "[NoHits] [bit] NOT NULL, "
                    + "[Language] [nvarchar](10) NOT NULL, "
                    + "[IndexName] [nvarchar](200) NOT NULL";
                internal const string Update = "UPDATE [" + TableName + "] "
                    + "SET [Searches] = [Searches]+1 "
                    + "WHERE [Query] = @query AND [Language] = @lang AND [IndexName] = @index";
                internal const string Insert = "INSERT INTO [" + TableName + "] "
                    + "([Query], [Searches], [NoHits], [Language], [IndexName])"
                    + "VALUES (@query, 1, @nohits, @lang, @index)";
                internal const string Delete = "DELETE FROM [" + TableName + "] "
                    + "WHERE Language = @lang AND [IndexName] = @index";
                internal const string Select = "SELECT [Query], [Searches] "
                    + "FROM [" + TableName + "] "
                    + "WHERE Language = @lang AND [IndexName] = @index";
                internal const string SelectNoHits = Select + " AND NoHits=1";
                internal const string Exists = "SELECT Query FROM [" + TableName + "] "
                    + "WHERE Query = @query AND Language = @lang AND [IndexName] = @index";
            }
        }
    }
}