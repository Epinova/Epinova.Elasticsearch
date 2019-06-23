using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core
{
    public static class Server
    {
        public static readonly ServerInfo Info = SetupInfo();
        public static readonly IEnumerable<Plugin> Plugins = SetupPlugins();

        private static IEnumerable<Plugin> SetupPlugins()
        {
            try
            {
                string uri = $"{GetHost()}/_cat/plugins?h=component,version&format=json";
                string json = HttpClientHelper.GetString(new Uri(uri));
                return JsonConvert.DeserializeObject<Plugin[]>(json);
            }
            catch(Exception ex)
            {
                LogManager.GetLogger(typeof(Server)).Error("Failed to get plugin info from server", ex);
                return new Plugin[0];
            }
        }

        private static string GetHost()
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

            return settings.Host;
        }

        private static ServerInfo SetupInfo()
        {
            try
            {
                string response = HttpClientHelper.GetString(new Uri(GetHost()));
                return JsonConvert.DeserializeObject<ServerInfo>(response);
            }
            catch(Exception ex)
            {
                LogManager.GetLogger(typeof(Server)).Error("Failed to get version info from server", ex);

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