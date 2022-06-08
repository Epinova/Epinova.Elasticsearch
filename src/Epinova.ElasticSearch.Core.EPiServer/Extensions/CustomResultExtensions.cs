using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    public static class CustomResultExtensions
    {
        private static readonly ITrackingRepository _trackingRepository = ServiceLocator.Current.GetInstance<ITrackingRepository>();

        public static async Task<CustomSearchResult<T>> GetCustomResultsAsync<T>(this IElasticSearchService<T> service)
        {
            CustomSearchResult<T> results = await service.GetResultsCustomAsync();
            HandleTracking(service, results.TotalHits);
            return results;
        }

        public static CustomSearchResult<T> GetCustomResults<T>(this IElasticSearchService<T> service)
        {
            CustomSearchResult<T> results = service.GetResultsCustom();
            HandleTracking(service, results.TotalHits);
            return results;
        }

        private static void HandleTracking<T>(this IElasticSearchService<T> service, int totalHits)
        {
            if(service.TrackSearch && !string.IsNullOrWhiteSpace(service.IndexName))
                _trackingRepository.AddSearch(service, noHits: totalHits == 0);
        }
    }
}
