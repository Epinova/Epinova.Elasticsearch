using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Settings
{
    public interface IElasticSearchSettings
    {
        int BulkSize { get; }
        long DocumentMaxSize { get; }
        bool EnableFileIndexing { get; }
        string Host { get; }
        string Username { get; }
        string Password { get; }
        string Index { get; }
        IEnumerable<string> Indices { get; }
        int ProviderMaxResults { get; }
        string GetCustomIndexName(string index, string language);
        string GetDefaultIndexName(string language);
        string GetCommerceIndexName(string language);
        string GetLanguage(string indexName);
        int CloseIndexDelay { get; }
        bool IgnoreXhtmlStringContentFragments { get; }
        int ClientTimeoutSeconds { get; }
        int NumberOfShards { get; }
        int NumberOfReplicas { get; }
        bool CommerceEnabled { get; }
        bool UseTls12 { get; }
    }
}