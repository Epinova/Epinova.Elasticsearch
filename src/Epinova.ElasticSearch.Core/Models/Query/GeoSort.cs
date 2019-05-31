namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class GeoSort : Sort
    {
        public (double Lat, double Lon) CompareTo { get; set; }

        public string Unit { get; set; }

        public string Mode { get; set; }
    }
}