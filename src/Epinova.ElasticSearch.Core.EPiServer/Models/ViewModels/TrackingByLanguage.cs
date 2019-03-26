using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class TrackingByLanguage : LanguageViewModelBase
    {
        public Dictionary<string, long> Searches { get; set; }
    }
}