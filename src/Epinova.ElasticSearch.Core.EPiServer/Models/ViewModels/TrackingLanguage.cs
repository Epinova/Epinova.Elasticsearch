using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class TrackingLanguage : LanguageViewModelBase
    {
        public Dictionary<string, long> Searches { get; internal set; } = new Dictionary<string, long>();

        public Dictionary<string, long> SearchesWithoutHits { get; internal set; } = new Dictionary<string, long>();
    }
}