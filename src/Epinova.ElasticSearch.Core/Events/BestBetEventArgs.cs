using System;

namespace Epinova.ElasticSearch.Core.Events
{
    public class BestBetEventArgs : EventArgs
    {
        public string Index { get; set; }
        public Type Type { get; set; }
        public string Id { get; set; }
        public string[] Terms { get; set; }
    }
}