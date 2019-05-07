using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class LanguageSynonyms : LanguageViewModelBase
    {
        public string Analyzer { get; set; }

        public List<Synonym> Synonyms { get; set; }

        public bool HasSynonymsFile { get; set; }
    }
}