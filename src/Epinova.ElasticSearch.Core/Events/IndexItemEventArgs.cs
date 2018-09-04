using System;

namespace Epinova.ElasticSearch.Core.Events
{
    public class IndexItemEventArgs : EventArgs
    {
        public string CallerInfo { get; set; }
        public object Item { get; set; }
        public Type Type { get; set; }
    }
}