using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IInspectorRepository
    {
        List<InspectItem> Search(string languageId, string searchText, int size, string type = null, string selectedIndex = null);
        Dictionary<string, List<TypeCount>> GetTypes(string languageId, string searchText, string selectedIndex = null);
    }
}