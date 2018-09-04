using System;
using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Conventions
{
    internal sealed class Suggestion
    {
        public Suggestion(Type type, bool includeAllFields = false)
        {
            Type = type;
            IncludeAllFields = includeAllFields;
            InputFields = new List<string>();
        }


        internal bool IncludeAllFields { get; set; }
        internal Type Type { get; }
        internal List<string> InputFields { get; }
    }
}