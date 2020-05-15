using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.EPiServer.Providers;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Extensions
{
    public static class CatalogExtensions
    {
        private static readonly ITrackingRepository TrackingRepository = ServiceLocator.Current.GetInstance<ITrackingRepository>();
        private static readonly IElasticSearchSettings ElasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

        private static string GetIndexName(CultureInfo searchLanguage)
            => $"{ElasticSearchSettings.Index}-{Constants.CommerceProviderName}-{Language.GetLanguageCode(searchLanguage)}";

        public static CatalogSearchResult<T> GetCatalogResults<T>(this IElasticSearchService<T> service)
            where T : EntryContentBase
        {
            service.UseIndex(GetIndexName(service.SearchLanguage));

            SearchResult results = service.GetResults();
            return GetCatalogSearchResult(service, results);
        }

        public static async Task<CatalogSearchResult<T>> GetCatalogResultsAsync<T>(this IElasticSearchService<T> service)
            where T : EntryContentBase
        {
            service.UseIndex(GetIndexName(service.SearchLanguage));

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
            {
                TrackingRepository.AddSearch(
                    Language.GetLanguageCode(service.SearchLanguage),
                    service.SearchText,
                    results.TotalHits == 0,
                    service.IndexName);
            }

            return new CatalogSearchResult<T>(results, hits);
        }
    }
}