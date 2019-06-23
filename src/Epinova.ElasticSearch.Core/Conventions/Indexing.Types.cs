using System;
using System.Collections.Generic;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        private static readonly List<Type> Types = new List<Type>();

        internal static Type[] ExcludedTypes => Types.ToArray();

        internal Indexing ExcludeType(Type type)
        {
            if(!Types.Contains(type))
            {
                Logger.Information($"Excluding type: {type.FullName}");
                Types.Add(type);
            }

            return this;
        }

        /// <summary>
        /// Excludes the specified type from the index. 
        /// See also <see cref="Attributes.ExcludeFromSearchAttribute"/>
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The <see cref="Indexing"/> instance</returns>
        public Indexing ExcludeType<T>()
            => ExcludeType(typeof(T));
    }
}