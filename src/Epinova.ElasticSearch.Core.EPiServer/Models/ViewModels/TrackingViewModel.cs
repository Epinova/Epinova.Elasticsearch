using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class TrackingViewModel : LanguageAwareViewModelBase
    {
        public TrackingViewModel(string currentLanguage) : base(currentLanguage)
        {
            SearchesByLanguage = new List<TrackingByLanguage>();
            SearchesWithoutHitsByLanguage = new List<TrackingByLanguage>();
        }

        public List<TrackingByLanguage> SearchesByLanguage { get; }

        public List<TrackingByLanguage> SearchesWithoutHitsByLanguage { get; }
    }
}