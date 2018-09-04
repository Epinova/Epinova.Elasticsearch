using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class AutoSuggestViewModel
    {
        public AutoSuggestViewModel(string currentLanguage)
        {
            WordsByLanguage = new List<LanguageAutoSuggestWords>();
            CurrentLanguage = currentLanguage;
        }


        public string CurrentLanguage { get; }

        public List<LanguageAutoSuggestWords> WordsByLanguage { get; }
    }
}