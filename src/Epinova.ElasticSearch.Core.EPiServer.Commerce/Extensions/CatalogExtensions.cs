using System.Collections.Generic;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.EPiServer.Providers;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Extensions
{
    public static class CatalogExtensions
    {
        private static readonly ITrackingRepository _trackingRepository = ServiceLocator.Current.GetInstance<ITrackingRepository>();
        private static readonly IElasticSearchSettings _elasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

        public static CatalogSearchResult<T> GetCatalogResults<T>(this IElasticSearchService<T> service) where T : EntryContentBase
        {
            service.UseIndex(_elasticSearchSettings.GetCommerceIndexName(service.SearchLanguage));

            SearchResult results = service.GetResults();
            return GetCatalogSearchResult(service, results);
        }

        public static async Task<CatalogSearchResult<T>> GetCatalogResultsAsync<T>(this IElasticSearchService<T> service) where T : EntryContentBase
        {
            service.UseIndex(_elasticSearchSettings.GetCommerceIndexName(service.SearchLanguage));

            SearchResult results = await service.GetResultsAsync();
            return GetCatalogSearchResult(service, results);
        }

        private static CatalogSearchResult<T> GetCatalogSearchResult<T>(IElasticSearchService<T> service, SearchResult results) where T : EntryContentBase
        {
            var hits = new List<CatalogSearchHit<T>>();

            foreach(SearchHit hit in results.Hits)
            {
                if(hit.ShouldAdd(false, out T content, new[] { ProviderConstants.CatalogProviderKey }, false))
                {
                    hits.Add(new CatalogSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight));
                }
                else
                {
                    results.TotalHits--;
                }
            }

            if(service.TrackSearch) 
                _trackingRepository.AddSearch(service, results.TotalHits == 0);

            return new CatalogSearchResult<T>(results, hits);
        }
    }
}