using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class TrackingViewModel
    {
        public TrackingViewModel(string currentLanguage)
        {
            SearchesByLanguage = new List<TrackingByLanguage>();
            SearchesWithoutHitsByLanguage = new List<TrackingByLanguage>();
            CurrentLanguage = currentLanguage;
        }


        public string CurrentLanguage { get; }

        public List<TrackingByLanguage> SearchesByLanguage { get; }

        public List<TrackingByLanguage> SearchesWithoutHitsByLanguage { get; }
    }
}