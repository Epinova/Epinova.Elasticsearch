using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface ITrackingRepository
    {
        void AddSearch<T>(IElasticSearchService<T> service, bool noHits);
        void Clear(string languageId, string index);
        IEnumerable<Tracking> GetSearches(string languageId, string index);
        IEnumerable<Tracking> GetSearchesWithoutHits(string languageId, string index);
    }
}