using System;

namespace Epinova.ElasticSearch.Core.Events
{
    public class BestBetEventArgs : EventArgs
    {
        public string Index { get; set; }
        public long Id { get; set; }
        public string[] Terms { get; set; }
    }
}