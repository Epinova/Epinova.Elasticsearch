using System;
using System.ComponentModel;

namespace Epinova.ElasticSearch.Core.Enums
{
    [Flags]
    internal enum MappingConflict
    {
        NotSet = 0x0000,

        [Description("Mapping is ok")]
        Found = 0x0001,

        [Description("Mapping is missing")]
        Missing = 0x0010,

        [Description("Mapping is in conflict")]
        Mapping = 0x0100,

        [Description("Mapping has different analyzer")]
        Analyzer = 0x1000,
    }
}