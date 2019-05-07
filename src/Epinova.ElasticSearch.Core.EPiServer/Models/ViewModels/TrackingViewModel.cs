using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class TrackingViewModel : LanguageAwareViewModelBase
    {
        public TrackingViewModel(string currentLanguage) : base(currentLanguage)
        {
        }

        public List<TrackingLanguage> Languages { get; } = new List<TrackingLanguage>();

        public void AddLanguage(string name, string id, Dictionary<string, string> indices, Dictionary<string, long> searches, Dictionary<string, long> searchesWithoutHits)
        {
            Languages.Add(new TrackingLanguage
            {
                LanguageId = id,
                LanguageName = name,
                Indices = indices,
                Searches = searches,
                SearchesWithoutHits = searchesWithoutHits
            });
        }
    }
}