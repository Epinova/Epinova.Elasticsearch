using System;

namespace Epinova.ElasticSearch.Core.Attributes
{
    /// <summary>
    /// Indicates that this property should be analyzed with a language-analyzer upon indexing, in essence enabling stemming. 
    /// Only works on string-types. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class StemAttribute : Attribute
    {
    }
}