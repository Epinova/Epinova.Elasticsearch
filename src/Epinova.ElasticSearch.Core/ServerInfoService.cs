using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core
{
    [ServiceConfiguration(ServiceType = typeof(IServerInfoService), Lifecycle = ServiceInstanceScope.Singleton)]
    internal class ServerInfoService : IServerInfoService
    {
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IElasticSearchSettings _settings;
        private ServerInfo _serverInfo;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(ServerInfoService));

        public ServerInfoService(IHttpClientHelper httpClientHelper, IElasticSearchSettings settings)
        {
            _httpClientHelper = httpClientHelper;
            _settings = settings;
        }

        public IEnumerable<Plugin> ListPlugins()
        {
            try
            {
                var uri = $"{_settings.Host}/_cat/plugins?h=component,version&format=json";
                var json = _httpClientHelper.GetString(new Uri(uri));
                return JsonConvert.DeserializeObject<Plugin[]>(json);
            }
            catch(Exception ex)
            {
                _logger.Error("Failed to get plugin info from server", ex);
                return Array.Empty<Plugin>();
            }
        }

        public ServerInfo GetInfo()
        {
            try
            {
                _serverInfo ??= JsonConvert.DeserializeObject<ServerInfo>(_httpClientHelper.GetString(new Uri(_settings.Host)));
                return _serverInfo;
            }
            catch(Exception ex)
            {
                _logger.Error("Failed to get version info from server", ex);

                return new ServerInfo
                {
                    Name = "#ERROR",
                    Cluster = "#ERROR",
                    ElasticVersion = new ServerInfo.InternalVersion
                    {
                        Number = "0.0.0",
                        LuceneVersion = "0.0.0"
                    }
                };
            }
        }
    }
}