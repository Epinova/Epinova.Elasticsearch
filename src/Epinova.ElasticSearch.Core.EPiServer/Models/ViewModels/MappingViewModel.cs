using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;
using Epinova.ElasticSearch.Core.Models.Admin;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class MappingViewModel : LanguageAwareViewModelBase
    {
        public MappingViewModel() : this(String.Empty)
        {
        }

        public MappingViewModel(string currentLanguage) : base(currentLanguage)
        {
        }

        public string SelectedIndex { get; set; }
        public List<IndexInformation> Indices { get; set; }
        public string Mappings { get; set; }
    }


}