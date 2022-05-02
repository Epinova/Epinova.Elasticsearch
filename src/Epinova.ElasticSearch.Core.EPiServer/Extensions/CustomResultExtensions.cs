using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    public static class CustomResultExtensions
    {
        private static readonly ITrackingRepository TrackingRepository = ServiceLocator.Current.GetInstance<ITrackingRepository>();

        public static async Task<CustomSearchResult<T>> GetCustomResultsAsync<T>(this IElasticSearchService<T> service)
        {
            CustomSearchResult<T> results = await service.GetResultsCustomAsync();

            if(service.TrackSearch && !string.IsNullOrWhiteSpace(service.IndexName))
            {
                TrackingRepository.AddSearch(service.SearchLanguage,
                    service.SearchText,
                    results.TotalHits == 0,
                    service.IndexName);
            }

            return results;
        }

        public static CustomSearchResult<T> GetCustomResults<T>(this IElasticSearchService<T> service)
        {
            CustomSearchResult<T> results = service.GetResultsCustom();
            
            if(service.TrackSearch && !string.IsNullOrWhiteSpace(service.IndexName))
            {
                TrackingRepository.AddSearch(service.SearchLanguage,
                    service.SearchText,
                    results.TotalHits == 0,
                    service.IndexName);
            }

            return results;
        }
    }
}
