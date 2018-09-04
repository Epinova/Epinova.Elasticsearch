using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface ITrackingRepository
    {
        void AddSearch(string languageId, string text, bool noHits);
        void Clear(string languageId);
        IEnumerable<Tracking> GetSearches(string languageId);
        IEnumerable<Tracking> GetSearchesWithoutHits(string languageId);
    }
}