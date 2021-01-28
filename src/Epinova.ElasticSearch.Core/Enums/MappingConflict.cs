using System;
using System.ComponentModel.DataAnnotations;

namespace Epinova.ElasticSearch.Core.Enums
{
    [Flags]
    internal enum MappingConflict
    {
        [Display(Description = "Mapping is ok")]
        Found = 1,
       
        [Display(Description = "Mapping is missing")]
        Missing = 2,
        
        [Display(Description = "Mapping is in conflict")]
        Mapping = 3,
        
        [Display(Description = "Mapping has different analyzer")]
        Analyzer = 4
    }
}