using System;
using System.Net;
using System.Text;
using Epinova.ElasticSearch.Core.Admin;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Settings;

namespace Epinova.ElasticSearch.Core.Utilities
{
    public class Indexing
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Indexing));
        private readonly IElasticSearchSettings _settings;

        public Indexing(IElasticSearchSettings settings)
        {
            _settings = settings;
        }

        public void DeleteIndex(string indexName)
        {
            var uri = $"{_settings.Host}/{indexName}";

            Logger.Information("Deleting index '" + indexName + "'");

            HttpClientHelper.Delete(new Uri(uri));
        }

        internal void CreateIndex(string indexName)
        {
            Logger.Information("Creating index '" + indexName + "'");

            var settings = new
            {
                settings = new
                {
                    number_of_shards = _settings.NumberOfShards > 0 ? _settings.NumberOfShards : 5,
                    number_of_replicas = _settings.NumberOfReplicas > 0 ? _settings.NumberOfReplicas : 1
                }
            };

            string json = Serialization.Serialize(settings);
            byte[] data = Encoding.UTF8.GetBytes(json);

            HttpClientHelper.Put(GetUri(indexName), data);
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

            var uri = $"{_settings.Host}/{indexName}{type}{endpoint}";

            return new Uri(uri);
        }
    }
}