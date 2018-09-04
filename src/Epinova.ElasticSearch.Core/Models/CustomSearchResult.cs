using System.Collections.Generic;
using System.Linq;

namespace Epinova.ElasticSearch.Core.Models
{
    /// <summary>
    /// Contains the materialization of the search query
    /// </summary>
    public sealed class CustomSearchResult<T> : SearchResultBase<CustomSearchHit<T>>
    {
        public CustomSearchResult()
        {
        }

        public CustomSearchResult(SearchResult searchResult, IEnumerable<CustomSearchHit<T>> filteredHits)
        {
            Query = searchResult.Query;
            Took = searchResult.Took;
            Facets = searchResult.Facets;
            Hits = filteredHits ?? Enumerable.Empty<CustomSearchHit<T>>();
            TotalHits = searchResult.TotalHits;
            DidYouMeanSuggestions = searchResult.DidYouMeanSuggestions;
        }
    }
}