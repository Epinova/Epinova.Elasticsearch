using System;
using System.Linq.Expressions;
using Epinova.ElasticSearch.Core.Conventions;
using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    public static class IndexingExtensions
    {
        /// <summary>
        /// Register a property as searchable. 
        /// <para>
        /// Use this if you can't alter the source code of the object to index, 
        /// thus preventing you from using [Searchable]
        /// </para> 
        /// </summary>
        public static Indexing IncludeProperty<T, TProperty>(this CustomPropertyConvention<T> instance, Expression<Func<T, TProperty>> fieldSelector)
            where T : IContent
        {
            string fieldName = ElasticSearchService<T>.GetFieldInfo(fieldSelector).Item1;
            Type type = typeof(T);

            if(!Indexing.Instance.SearchableProperties.ContainsKey(type))
            {
                Indexing.Instance.SearchableProperties.TryAdd(type, new[] { fieldName });
            }
            else
            {
                if(Indexing.Instance.SearchableProperties.TryGetValue(type, out string[] current))
                {
                    var merged = new string[current.Length + 1];
                    current.CopyTo(merged, 1);
                    merged[0] = fieldName;
                    Indexing.Instance.SearchableProperties[type] = merged;
                }
            }

            return Indexing.Instance;
        }
    }
}