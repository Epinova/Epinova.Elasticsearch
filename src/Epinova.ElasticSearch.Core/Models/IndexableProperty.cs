using System;

namespace Epinova.ElasticSearch.Core.Models
{
    internal sealed class IndexableProperty
    {
        public string Name { get; set; }

        public Type Type { get; set; }

        public bool Analyzable { get; set; }

        public bool IncludeInDidYouMean { get; set; }
    }
}