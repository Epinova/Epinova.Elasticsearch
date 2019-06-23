using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    public class BoostingInputModel
    {
        public BoostingInputModel()
        {
            Boosting = new Dictionary<string, int>();
        }

        public string TypeName { get; set; }

        public Dictionary<string, int> Boosting { get; set; }
    }
}