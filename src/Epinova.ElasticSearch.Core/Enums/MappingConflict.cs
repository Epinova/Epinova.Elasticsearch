using System;

namespace Epinova.ElasticSearch.Core.Enums
{
   [Flags]
    internal enum MappingConflict
    {
        Found = 0,
        Missing = 1,
        Mapping = 2,
        Analyzer = 4
    }
}