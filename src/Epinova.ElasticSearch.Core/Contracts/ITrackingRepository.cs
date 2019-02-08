using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface ITrackingRepository
    {
        void AddSearch(string languageId, string text, bool noHits, string index);
        void Clear(string languageId, string index);
        IEnumerable<Tracking> GetSearches(string languageId, string index);
        IEnumerable<Tracking> GetSearchesWithoutHits(string languageId, string index);
    }
}