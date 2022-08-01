using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class InspectViewModel
    {
        public string SearchText { get; set; }

        public bool Analyzed { get; set; }

        public Dictionary<string, List<TypeCount>> TypeCounts { get; set; }
        public string SelectedType { get; set; }

        public int SelectedNumberOfItems { get; set; }
        public List<int> NumberOfItems { get; set; }

        public List<InspectItem> SearchHits { get; set; }
        public List<KeyValuePair<string, string>> Indices { get; set; }
        public string SelectedIndex { get; set; }
        public string SelectedIndexName { get; set; }
    }
}