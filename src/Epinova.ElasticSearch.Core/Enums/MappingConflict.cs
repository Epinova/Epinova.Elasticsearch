using System;
using System.ComponentModel.DataAnnotations;

namespace Epinova.ElasticSearch.Core.Enums
{
    [Flags]
    internal enum MappingConflict
    {
        NotSet = 0x0000,

        [Display(Name = "Mapping is ok")]
        Found = 0x0001,
       
        [Display(Name = "Mapping is missing")]
        Missing = 0x0010,

        [Display(Name = "Mapping is in conflict")]
        Mapping = 0x0100,
        
        [Display(Name = "Mapping has different analyzer")]
        Analyzer = 0x1000,
    }
}