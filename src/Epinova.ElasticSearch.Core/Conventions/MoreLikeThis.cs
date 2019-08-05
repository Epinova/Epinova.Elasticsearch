using System.Collections.Concurrent;
using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.Conventions
{
    /// <summary>
    /// Contains methods for configuring More Like This
    /// </summary>
    public sealed class MoreLikeThis
    {
        private MoreLikeThis()
        {
            AddComponentField(nameof(IContent.Name));
        }

        /// <summary>
        /// Add field to be searched in the More Like This component/widget
        /// <para>Default value is "Name"</para>
        /// </summary>
        public void AddComponentField(string name)
            => ComponentFields.TryAdd(name, true);

        internal static ConcurrentDictionary<string, bool> ComponentFields { get; }
            = new ConcurrentDictionary<string, bool>();

        /// <summary>
        /// The singleton instance property
        /// </summary>
        public static MoreLikeThis Instance { get; } = new MoreLikeThis();
    }
}