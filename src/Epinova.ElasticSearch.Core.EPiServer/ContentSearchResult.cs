using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Models;
using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.EPiServer
{
    /// <summary>
    /// Contains the materialization of the search query
    /// </summary>
    public sealed class ContentSearchResult<T> : SearchResultBase<ContentSearchHit<T>> where T : IContentData
    {
        public ContentSearchResult(SearchResult searchResult, IEnumerable<ContentSearchHit<T>> filteredHits)
        {
            Query = searchResult.Query;
            Took = searchResult.Took;
            Facets = searchResult.Facets;
            Hits = filteredHits ?? Enumerable.Empty<ContentSearchHit<T>>();
            TotalHits = searchResult.TotalHits;
            DidYouMeanSuggestions = searchResult.DidYouMeanSuggestions;
        }
    }
}