using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.Settings
{
    [ServiceConfiguration(typeof(IElasticSearchSettings), Lifecycle = ServiceInstanceScope.Singleton)]
    public class ElasticSearchSettings : IElasticSearchSettings
    {
        private readonly ElasticSearchSection _configuration;
        private bool? _commerceEnabled;

        public ElasticSearchSettings()
        {
            _configuration = ElasticSearchSection.GetConfiguration();
        }

        public string Index => _configuration.IndicesParsed.Count() == 1
            ? _configuration.IndicesParsed.First().Name
            : _configuration.IndicesParsed.Single(i => i.Default).Name;

        public IEnumerable<string> Indices => _configuration.IndicesParsed.Select(i => i.Name);

        public bool CommerceEnabled
        {
            get
            {
                if(_commerceEnabled.HasValue)
                {
                    return _commerceEnabled.Value;
                }

                try
                {
                    _commerceEnabled = Assembly.Load("Epinova.ElasticSearch.Core.EPiServer.Commerce") != null;
                }
                catch
                {
                    _commerceEnabled = false;
                }

                return _commerceEnabled.Value;
            }
        }

        /// <summary>
        /// Default delay is 500 ms if attribute closeIndexDelay is not set in epinova.elasticsearch element i web.config
        /// </summary>
        public int CloseIndexDelay => _configuration.CloseIndexDelay;

        public string Host => _configuration.Host?.TrimEnd('/');

        public string Username => _configuration.Username;

        public string Password => _configuration.Password;

        public long DocumentMaxSize => _configuration.Files.ParsedMaxsize;

        public int BulkSize => _configuration.Bulksize;

        public int NumberOfShards => _configuration.NumberOfShards;

        public int NumberOfReplicas => _configuration.NumberOfReplicas;

        public int ProviderMaxResults => _configuration.ProviderMaxResults;

        public bool EnableFileIndexing => _configuration.Files.Enabled;

        public bool IgnoreXhtmlStringContentFragments => _configuration.IgnoreXhtmlStringContentFragments;

        /// <summary>
        /// Default HttpClient.Timeout is 100 seconds if attribute clientTimeoutSeconds is not set in epinova.elasticsearch element i web.config
        /// https://msdn.microsoft.com/en-us/library/system.net.http.httpclient.timeout(v=vs.110).aspx
        /// </summary>
        public int ClientTimeoutSeconds => _configuration.ClientTimeoutSeconds;

        public bool UseTls12 =>  _configuration.UseTls12;

        public string GetDefaultIndexName(string language)
        {
            if(String.IsNullOrWhiteSpace(language))
            {
                throw new InvalidOperationException("Language must be specified");
            }

            return CreateIndexName(Index, language);
        }

        public string GetCommerceIndexName(string language)
        {
            return CreateIndexName($"{Index}-{Constants.CommerceProviderName}", language);
        }

        public string GetCustomIndexName(string index, string language)
        {
            if(String.IsNullOrWhiteSpace(index))
            {
                throw new InvalidOperationException("IndexInformation is null");
            }

            if(String.IsNullOrWhiteSpace(language))
            {
                throw new InvalidOperationException("Language must be specified");
            }

            return CreateIndexName(index, language);
        }

        private static string CreateIndexName(string index, string language)
        {
            if(String.IsNullOrWhiteSpace(index))
            {
                throw new InvalidOperationException("Index must be specified");
            }

            if(String.IsNullOrWhiteSpace(language))
            {
                throw new InvalidOperationException("Language must be specified");
            }

            return $"{index}-{language}".ToLower();
        }

        public string GetLanguage(string indexName)
        {
            if(String.IsNullOrWhiteSpace(indexName))
            {
                throw new InvalidOperationException("Index must be specified");
            }

            if(!indexName.Contains("-"))
            {
                throw new InvalidOperationException("Invalid index name '" + indexName + "' (Must be <name>-<lang>)");
            }

            return indexName.Split('-').Last();
        }
    }
}