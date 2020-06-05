using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core
{
    [ServiceConfiguration(ServiceType = typeof(IElasticSearchService), Lifecycle = ServiceInstanceScope.Transient)]
    public class ElasticSearchService : ElasticSearchService<object>, IElasticSearchService
    {
        public ElasticSearchService(
            IServerInfoService serverInfoService,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper)
            : base(serverInfoService, settings, httpClientHelper)
        {
        }
    }
}