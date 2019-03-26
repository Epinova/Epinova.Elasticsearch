using System;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions
{
    public abstract class LanguageAwareViewModelBase
    {
        protected LanguageAwareViewModelBase(string currentLanguage)
        {
            CurrentLanguage = currentLanguage ?? String.Empty;
        }

        public string CurrentLanguage { get; }
    }
}