using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class ConsoleViewModel
    {
        public string SelectedIndex { get; }
        public List<string> Indices { get; }
        public string Query { get; }
        public string Result { get; set; }


        public ConsoleViewModel(string selectedIndex, List<string> indices, string query)
        {
            SelectedIndex = selectedIndex;
            Indices = indices;
            Query = query;
        }
    }
}