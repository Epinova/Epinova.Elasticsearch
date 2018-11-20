using System.Collections.Generic;
using EPiServer.Commerce.Catalog.ContentTypes;

namespace Epinova.ElasticSearch.Core.EPiServer
{
    public sealed class CatalogSearchHit<T>
        where T : EntryContentBase
    {
        public CatalogSearchHit(T content, Dictionary<string, object> custom, double queryScore, string highlight)
        {
            Content = content;
            Custom = custom;
            Score = queryScore;
            Highlight = highlight;
        }

        public T Content { get; }

        public Dictionary<string, object> Custom { get; }

        public double Score { get; }

        public string Highlight { get; }
    }
}