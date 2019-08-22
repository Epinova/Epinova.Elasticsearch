using System;

namespace Epinova.ElasticSearch.Core.Attributes
{
    /// <summary>
    /// Indicates that this property should be included as a source to Did You Mean queries. 
    /// Only works on string-types. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DidYouMeanSourceAttribute : Attribute
    {
    }
}