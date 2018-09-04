using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class LanguageAutoSuggestWords
    {
        public string LanguageId { get; set; }

        public string LanguageName { get; set; }

        public List<string> Words { get; set; }
    }
}