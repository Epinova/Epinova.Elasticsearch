using System.Collections.Generic;
using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.EPiServer
{
    public sealed class ContentSearchHit<T>
        where T : IContentData
    {
        public ContentSearchHit(T content, Dictionary<string, object> custom, double queryScore, string highlight)
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