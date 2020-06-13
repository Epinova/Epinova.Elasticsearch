using System;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Admin
{
    internal class Health
    {
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;

        public Health(
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper)
        {
            _settings = settings;
            _httpClientHelper = httpClientHelper;
        }

        public virtual HealthInformation GetClusterHealth()
        {
            var uri = $"{_settings.Host}/_cat/health?format=json";
            var json = _httpClientHelper.GetJson(new Uri(uri));

            return GetClusterHealth(json);
        }

        internal static HealthInformation GetClusterHealth(string json)
            => JsonConvert.DeserializeObject<HealthInformation[]>(json).FirstOrDefault();

        public virtual Node[] GetNodeInfo()
        {
            var uri = $"{_settings.Host}/_cat/nodes?format=json&h=m,v,http,d,rc,rm,u,n";
            var json = _httpClientHelper.GetJson(new Uri(uri));

            return GetNodeInfo(json);
        }

        internal static Node[] GetNodeInfo(string json)
            => JsonConvert.DeserializeObject<Node[]>(json);
    }
}
