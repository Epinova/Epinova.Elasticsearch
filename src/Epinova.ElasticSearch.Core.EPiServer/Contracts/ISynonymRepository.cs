using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models;

namespace Epinova.ElasticSearch.Core.EPiServer.Contracts
{
    public interface ISynonymRepository
    {
        void SetSynonyms(string languageId, string analyzer, List<Synonym> synonymsToAdd, string index);
        List<Synonym> GetSynonyms(string languageId, string index);
        string GetSynonymsFilePath(string languageId, string index);
    }
}