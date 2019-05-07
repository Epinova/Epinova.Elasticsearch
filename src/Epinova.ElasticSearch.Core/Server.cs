using System;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core
{
    public static class Server
    {
        private static IElasticSearchSettings _settings;

        public static ServerInfo Info;

        public static Plugin[] Plugins;

        static Server()
        {
            ServiceLocator.Current.TryGetExistingInstance(out _settings);

            SetupInfo();
            SetupPlugins();
        }

        private static void SetupPlugins()
        {
            try
            {
                string uri = $"{GetHost()}/_cat/plugins?h=component,version&format=json";
                string json = HttpClientHelper.GetString(new Uri(uri));
                Plugins = JsonConvert.DeserializeObject<Plugin[]>(json);
            }
            catch (Exception ex)
            {
                Plugins = new Plugin[0];
                LogManager.GetLogger(typeof(Server)).Error("Failed to get plugin info from server", ex);
            }
        }

        private static string GetHost()
        {
            if (_settings == null)
                _settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

            return _settings.Host;
        }

        private static void SetupInfo()
        {
            try
            {
                string response = HttpClientHelper.GetString(new Uri(GetHost()));

                ServerInfo info = JsonConvert.DeserializeObject<ServerInfo>(response);

                Info = info;
            }
            catch (Exception ex)
            {
                Info = new ServerInfo
                {
                    Name = "#ERROR",
                    Cluster = "#ERROR",
                    ElasticVersion = new ServerInfo.InternalVersion
                    {
                        Number = "0.0.0",
                        LuceneVersion = "0.0.0"
                    }
                };

                LogManager.GetLogger(typeof(Server)).Error("Failed to get version info from server", ex);
            }
        }
    }
}