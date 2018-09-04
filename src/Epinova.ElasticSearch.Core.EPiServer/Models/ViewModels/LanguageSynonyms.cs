using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class LanguageSynonyms
    {
        public string Analyzer { get; set; }

        public string LanguageId { get; set; }

        public string LanguageName { get; set; }

        public List<Synonym> Synonyms { get; set; }
    }
}