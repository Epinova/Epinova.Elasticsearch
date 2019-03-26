using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class LanguageAutoSuggestWords : LanguageViewModelBase
    {
        public List<string> Words { get; set; }
    }
}