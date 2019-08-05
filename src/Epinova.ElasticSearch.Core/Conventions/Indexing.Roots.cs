using System.Collections.Generic;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        internal static readonly List<int> Roots = new List<int>();

        internal static int[] ExcludedRoots => Roots.ToArray();

        /// <summary>
        /// Excludes the specified root id and its children from the index. 
        /// </summary>
        /// <returns>The <see cref="Indexing"/> instance</returns>
        public Indexing ExcludeRoot(int rootId)
        {
            if(!Roots.Contains(rootId))
            {
                Logger.Information($"Excluding root: {rootId}");
                Roots.Add(rootId);
            }

            return this;
        }
    }
}