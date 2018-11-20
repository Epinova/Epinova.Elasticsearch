using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        internal readonly ConcurrentDictionary<Type, string[]> SearchableProperties;

        internal static List<CustomProperty> CustomProperties { get; }

        /// <summary>
        /// Add a convention for the specified type
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>An instance of <see cref="CustomPropertyConvention{T}"/></returns>
        public CustomPropertyConvention<T> ForType<T>()
        {
            return new CustomPropertyConvention<T>(this);
        }
    }
}