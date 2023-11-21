using System;
using System.Collections.Generic;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        private static readonly List<Type> _listExcludedTypes = new List<Type>();
        private static readonly List<Type> _listIncludedTypes = new List<Type>();

        internal static Type[] ExcludedTypes => _listExcludedTypes.ToArray();
        internal static Type[] IncludedTypes => _listIncludedTypes.ToArray();

        internal Indexing ExcludeType(Type type)
        {
            if(!_listExcludedTypes.Contains(type))
            {
                Logger.Information($"Excluding type: {type.FullName}");
                _listExcludedTypes.Add(type);
            }

            return this;
        }

        internal Indexing IncludeType(Type type)
        {
            if(!_listIncludedTypes.Contains(type))
            {
                Logger.Information($"Include type: {type.FullName}");
                _listIncludedTypes.Add(type);
            }

            return this;
        }

        /// <summary>
        /// Excludes the specified type from the index. 
        /// See also <see cref="Attributes.ExcludeFromSearchAttribute"/>
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The <see cref="Indexing"/> instance</returns>
        public Indexing ExcludeType<T>() => ExcludeType(typeof(T));

        /// <summary>
        /// Explicit indexing of type.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The <see cref="Indexing"/> instance</returns>
        public Indexing IncludeType<T>() => IncludeType(typeof(T));
    }
}