namespace Epinova.ElasticSearch.Core.EPiServer.Models
{
    public class Synonym
    {
        public string From { get; set; }
        public string To { get; set; }
        public bool TwoWay { get; set; }
        public bool MultiWord { get; set; }
    }
}