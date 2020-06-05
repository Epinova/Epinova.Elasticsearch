using System;
using System.Net;
using System.Text;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Utilities
{
    public class Indexing
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(Indexing));
        private readonly IServerInfoService _serverInfoService;
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;

        public Indexing(
            IServerInfoService serverInfoService,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper)
        {
            _serverInfoService = serverInfoService;
            _settings = settings;
            _httpClientHelper = httpClientHelper;
        }

        public void DeleteIndex(string indexName)
        {
            var uri = $"{_settings.Host}/{indexName}";

            _logger.Information("Deleting index '" + indexName + "'");

            _httpClientHelper.Delete(new Uri(uri));
        }

        internal void CreateIndex(string indexName)
        {
            _logger.Information("Creating index '" + indexName + "'");

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

            _httpClientHelper.Put(GetUri(indexName), data);
        }

        public bool IndexExists(string indexName)
        {
            HttpStatusCode status = _httpClientHelper.Head(GetUri(indexName));

            return status == HttpStatusCode.OK;
        }

        internal void Open(string indexName)
        {
            _logger.Information("Opening index");

            _httpClientHelper.Post(GetUri(indexName, "_open"), (byte[])null);

            var index = new Index(_serverInfoService, _settings, _httpClientHelper, indexName);
            index.WaitForStatus();
        }

        internal void Close(string indexName)
        {
            _logger.Information($"Closing index with delay of {_settings.CloseIndexDelay} ms");

            _httpClientHelper.Post(GetUri(indexName, "_close"), (byte[])null);

            var index = new Index(_serverInfoService, _settings, _httpClientHelper, indexName);
            index.WaitForStatus();
        }

        internal Uri GetUri(string indexName, string endpoint = null, string type = null, string extraParams = null)
        {
            type = type != null ? String.Concat("/", type) : null;
            endpoint = endpoint != null ? String.Concat("/", endpoint) : null;

            var uri = $"{_settings.Host}/{indexName}{type}{endpoint}";

            if(extraParams != null)
            {
                uri += (uri.Contains("?") ? "&" : "?") + extraParams;
            }

            return new Uri(uri);
        }
    }
}