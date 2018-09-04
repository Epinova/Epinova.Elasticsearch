using System;

namespace Epinova.ElasticSearch.Core.Attributes
{
    /// <summary>
    /// Excludes type from Elastic Search
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ExcludeFromSearchAttribute : Attribute
    {
    }
}
