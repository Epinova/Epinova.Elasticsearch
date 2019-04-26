using System.Collections.Generic;
using System.Globalization;
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
        private static readonly ITrackingRepository TrackingRepository;
        private static readonly IElasticSearchSettings ElasticSearchSettings;

        static CatalogExtensions()
        {
            TrackingRepository = ServiceLocator.Current.GetInstance<ITrackingRepository>();
            ElasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        }

        private static string GetIndexName(CultureInfo searchLanguage)
        {
            return $"{ElasticSearchSettings.Index}-{Core.Constants.CommerceProviderName}-{Language.GetLanguageCode(searchLanguage)}";
        }

        public static CatalogSearchResult<T> GetCatalogResults<T>(this IElasticSearchService<T> service)
            where T : EntryContentBase
        {
            service.SearchType = typeof(T);
            service.UseIndex(GetIndexName(service.SearchLanguage));

            SearchResult results = service.GetResults();
            var hits = new List<CatalogSearchHit<T>>();

            foreach (SearchHit hit in results.Hits)
            {
                if (hit.ShouldAdd(false, out T content, new[] { ProviderConstants.CatalogProviderKey }))
                    hits.Add(new CatalogSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight));
                else
                    results.TotalHits--;
            }

            if (service.TrackSearch)
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