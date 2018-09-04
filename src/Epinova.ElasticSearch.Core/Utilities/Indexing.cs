using System;
using System.Net;
using Epinova.ElasticSearch.Core.Admin;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Settings;

namespace Epinova.ElasticSearch.Core.Utilities
{
    public class Indexing
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Indexing));
        private static IElasticSearchSettings _settings;

        public Indexing(IElasticSearchSettings settings)
        {
            _settings = settings;
        }


        public void DeleteIndex(string indexName)
        {
            string uri = $"{_settings.Host}/{indexName}";

            Logger.Information("Deleting index '" + indexName + "'");

            HttpClientHelper.Delete(new Uri(uri));
        }

        internal void CreateIndex(string indexName)
        {
            Logger.Information("Creating index '" + indexName + "'");

            HttpClientHelper.Put(GetUri(indexName));
        }

        public bool IndexExists(string indexName)
        {
            HttpStatusCode status = HttpClientHelper.Head(GetUri(indexName));

            return status == HttpStatusCode.OK;
        }

        internal void Open(string indexName)
        {
            Logger.Information("Opening index");

            HttpClientHelper.Post(GetUri(indexName, "_open"));

            var index = new Index(_settings, indexName);
            index.WaitForStatus();
        }

        internal void Close(string indexName)
        {
            Logger.Information($"Closing index with delay of {_settings.CloseIndexDelay} ms");

            HttpClientHelper.Post(GetUri(indexName, "_close"));

            var index = new Index(_settings, indexName);
            index.WaitForStatus();
        }

        internal Uri GetUri(string indexName, string endpoint = null, string type = null)
        {
            type = type != null ? String.Concat("/", type) : null;
            endpoint = endpoint != null ? String.Concat("/", endpoint) : null;

            string uri = $"{_settings.Host}/{indexName}{type}{endpoint}";

            return new Uri(uri);
        }
    }
}