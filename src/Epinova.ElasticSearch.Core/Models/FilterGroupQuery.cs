using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models
{
    internal sealed class FilterGroupQuery
    {
        public FilterGroupQuery(Operator @operator = Operator.And)
        {
            Operator = @operator;
            Filters = new List<Filter>();
        }


        [JsonIgnore]
        public List<Filter> Filters { get; private set; }

        [JsonIgnore]
        public Operator Operator { get; private set; }
    }
}