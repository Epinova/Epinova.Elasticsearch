using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models.Admin;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class AdminViewModel
    {
        public AdminViewModel(HealthInformation clusterHealth, IEnumerable<IndexInformation> allIndexes, Node[] nodeInfo)
        {
            NodeInfo = nodeInfo;
            ClusterHealth = clusterHealth;
            AllIndexes = allIndexes;
        }

        public Node[] NodeInfo { get; }
        public HealthInformation ClusterHealth { get; }
        public IEnumerable<IndexInformation> AllIndexes { get; }
    }
}