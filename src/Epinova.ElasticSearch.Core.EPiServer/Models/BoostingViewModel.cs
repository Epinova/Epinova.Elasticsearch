using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    public class BoostingViewModel
    {
        public BoostingViewModel()
        {
            BoostingByType = new Dictionary<string, List<BoostItem>>();
        }

        public Dictionary<string, List<BoostItem>> BoostingByType { get; }
    }
}