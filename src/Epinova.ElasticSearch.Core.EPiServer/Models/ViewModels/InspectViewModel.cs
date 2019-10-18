using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class InspectViewModel : LanguageAwareViewModelBase
    {
        public InspectViewModel() : this(String.Empty)
        {
        }

        public InspectViewModel(string currentLanguage) : base(currentLanguage)
        {
        }

        public List<InspectLanguage> Languages { get; } = new List<InspectLanguage>();

        public string SearchText { get; set; }

        public bool Analyzed { get; set; }

        public void AddLanguage(string name, string id, Dictionary<string, string> indices)
        {
            Languages.Add(new InspectLanguage
            {
                LanguageId = id,
                LanguageName = name,
                Indices = indices
            });
        }

        public Dictionary<string, List<TypeCount>> TypeCounts { get; set; }
        public string SelectedType { get; set; }

        public int SelectedNumberOfItems { get; set; }
        public List<int> NumberOfItems { get; set; }

        public List<InspectItem> SearchHits { get; set; }
    }
}