using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class SettingsViewModel
    {
        public string SelectedIndex { get; }
        public List<string> Indices { get; }
        public string Result { get; set; }

        public SettingsViewModel(string selectedIndex, List<string> indices)
        {
            SelectedIndex = selectedIndex;
            Indices = indices;
        }
    }
}