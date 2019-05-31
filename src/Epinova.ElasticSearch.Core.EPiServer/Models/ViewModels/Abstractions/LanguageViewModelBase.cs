using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions
{
    public abstract class LanguageViewModelBase
    {
        public string LanguageId { get; set; }

        public string LanguageName { get; set; }

        public string IndexName { get; set; }

        public Dictionary<string, string> Indices { get; internal set; } = new Dictionary<string, string>();
    }
}