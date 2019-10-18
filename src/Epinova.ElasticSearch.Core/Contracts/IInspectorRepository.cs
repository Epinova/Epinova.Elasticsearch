using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IInspectorRepository
    {
        List<InspectItem> Search(string searchText, bool analyzed, string language, string indexName, int size, string type = null, string selectedIndex = null);
        Dictionary<string, List<TypeCount>> GetTypes(string searchText, string indexName);
    }
}