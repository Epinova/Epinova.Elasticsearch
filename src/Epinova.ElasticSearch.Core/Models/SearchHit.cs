using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models.Serialization;

namespace Epinova.ElasticSearch.Core.Models
{
    public class SearchHit : IndexItem
    {
        internal SearchHit(Hit hit)
        {
            Id = hit.Source.Id;
            ParentId = hit.Source.ParentId;
            Lang = hit.Source.Lang;
            Title = hit.Source.Title;
            Name = hit.Source.Name;
            Updated = hit.Source.Updated;
            Type = hit.Source.Type;
            Types = hit.Source.Types;
            QueryScore = hit.Score;
            Highlight = hit.Highlight;
            CustomProperties = new Dictionary<string, object>();
        }

        public Dictionary<string, object> CustomProperties { get; set; }

        public double QueryScore { get; private set; }

        public string Highlight { get; private set; }
    }
}