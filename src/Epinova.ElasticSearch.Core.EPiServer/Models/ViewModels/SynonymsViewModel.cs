using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class SynonymsViewModel : LanguageAwareViewModelBase
    {
        public SynonymsViewModel(string currentLanguage) : base(currentLanguage)
        {
            SynonymsByLanguage = new List<LanguageSynonyms>();
        }

        public List<LanguageSynonyms> SynonymsByLanguage { get; }
    }
}