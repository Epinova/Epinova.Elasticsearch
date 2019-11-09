using System;

namespace Epinova.ElasticSearch.Core
{
    public static class Constants
    {
        public const string EPiServerConnectionStringName = "EPiServerDB";
        public const string TrackingFieldIndex = "IndexName";
        public const string CommerceProviderName = "CatalogContent";
        public const string IndexEPiServerContentDisplayName = "Elasticsearch: Index CMS content";

        // There was a breaking change in v5.6 renaming the "inline" field to "source" in scripts
        internal static readonly Version InlineVsSourceVersion = new Version(5, 6);

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
                internal static readonly string Update = "UPDATE [" + TableName + "] "
                    + "SET [Searches] = [Searches]+1 "
                    + "WHERE [Query] = @query AND [Language] = @lang AND [IndexName] = @index";
                internal static readonly string Insert = "INSERT INTO [" + TableName + "] "
                    + "([Query], [Searches], [NoHits], [Language], [IndexName])"
                    + "VALUES (@query, 1, @nohits, @lang, @index)";
                internal static readonly string Delete = "DELETE FROM [" + TableName + "] "
                    + "WHERE Language = @lang AND [IndexName] = @index";
                internal static readonly string Select = "SELECT [Query], [Searches] "
                    + "FROM [" + TableName + "] "
                    + "WHERE Language = @lang AND [IndexName] = @index";
                internal static readonly string SelectNoHits = Select + " AND NoHits=1";
                internal static readonly string Exists = "SELECT Query FROM [" + TableName + "] "
                    + "WHERE Query = @query AND Language = @lang AND [IndexName] = @index";


            }
        }
    }
}