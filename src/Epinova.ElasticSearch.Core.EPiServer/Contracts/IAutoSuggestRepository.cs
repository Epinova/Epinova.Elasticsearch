using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Contracts
{
    public interface IAutoSuggestRepository
    {
        void AddWord(string languageId, string word);
        void DeleteWord(string languageId, string word);
        List<string> GetWords(string languageId);
    }
}