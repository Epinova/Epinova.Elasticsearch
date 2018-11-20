using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Models;
using EPiServer.Commerce.Catalog.ContentTypes;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce
{
    /// <summary>
    /// Contains the materialization of the search query
    /// </summary>
    public sealed class CatalogSearchResult<T> : SearchResultBase<CatalogSearchHit<T>>
        where T : EntryContentBase
    {
        public CatalogSearchResult(SearchResult searchResult, IEnumerable<CatalogSearchHit<T>> filteredHits)
        {
            Query = searchResult.Query;
            Took = searchResult.Took;
            Facets = searchResult.Facets;
            Hits = filteredHits ?? Enumerable.Empty<CatalogSearchHit<T>>();
            TotalHits = searchResult.TotalHits;
            DidYouMeanSuggestions = searchResult.DidYouMeanSuggestions;
        }
    }
}