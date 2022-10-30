using System.Collections.Generic;
using System.Globalization;

namespace Epinova.ElasticSearch.Core.Settings
{
    public interface IElasticSearchSettings
    {
        int BulkSize { get; }
        long DocumentMaxSize { get; }
        bool EnableFileIndexing { get; }
        bool DisableContentIndexing { get; }
        string Host { get; }
        string Username { get; }
        string Password { get; }
        string Index { get; }
        IEnumerable<string> Indices { get; }
        int ProviderMaxResults { get; }
        string GetCommerceIndexName(CultureInfo language);
        string GetCustomIndexName(string index, CultureInfo language);
        string GetDefaultIndexName(CultureInfo language);
        string GetLanguageFromIndexName(string indexName);
        string GetIndexNameWithoutLanguage(string indexName);
        int CloseIndexDelay { get; }
        bool IgnoreXhtmlStringContentFragments { get; }
        int ClientTimeoutSeconds { get; }
        int NumberOfShards { get; }
        int NumberOfReplicas { get; }
        bool CommerceEnabled { get; }
        bool UseTls12 { get; }
    }
}