using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class SynonymsViewModel
    {
        public SynonymsViewModel(string currentLanguage)
        {
            SynonymsByLanguage = new List<LanguageSynonyms>();
            CurrentLanguage = currentLanguage;
        }


        public string CurrentLanguage { get; }

        public List<LanguageSynonyms> SynonymsByLanguage { get; }
    }
}