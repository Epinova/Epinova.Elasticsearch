namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    public class BoostItem
    {
        public string TypeName { get; set; }
        public string GroupName { get; set; }
        public string DisplayName { get; set; }
        public int Weight { get; set; }
    }
}