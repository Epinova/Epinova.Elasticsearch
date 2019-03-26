using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class AutoSuggestViewModel : LanguageAwareViewModelBase
    {
        public AutoSuggestViewModel(string currentLanguage) : base(currentLanguage)
        {
            WordsByLanguage = new List<LanguageAutoSuggestWords>();
        }

        public List<LanguageAutoSuggestWords> WordsByLanguage { get; }
    }
}