using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class TrackingByLanguage
    {
        public string LanguageId { get; set; }

        public string LanguageName { get; set; }

        public Dictionary<string, long> Searches { get; set; }
    }
}