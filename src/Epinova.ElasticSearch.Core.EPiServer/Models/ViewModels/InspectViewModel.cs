using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class InspectViewModel
    {
        public string SearchText { get; set; }

        public List<string> Indices { get; set; }
        public string SelectedIndex { get; set; }

        public IList<LanguageBranch> Languages { get; set; }
        public string SelectedLanguage { get; set; }

        public Dictionary<string, List<TypeCount>> TypeCounts { get; set; }
        public string SelectedType { get; set; }

        public int SelectedNumberOfItems { get; set; }
        public List<int> NumberOfItems { get; set; }

        public List<InspectItem> SearchHits { get; set; }
    }
}