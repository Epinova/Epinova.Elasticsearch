using System.Collections.Generic;
using System.Globalization;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface ITrackingRepository
    {
        void AddSearch(CultureInfo language, string text, bool noHits, string indexName);
        void Clear(string languageId, string index);
        IEnumerable<Tracking> GetSearches(string languageId, string index);
        IEnumerable<Tracking> GetSearchesWithoutHits(string languageId, string index);
    }
}