using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        internal static int HighlightFragmentSize { get; private set; } = 150;
        internal static string HighlightTag { get; private set; } = "mark";

        /// <summary>
        /// Sets how many characters the highlighted excerpt should return
        /// <para>Defaults to 150</para>
        /// </summary>
        public void SetHighlightFragmentSize(int size) =>
            HighlightFragmentSize = size;

        /// <summary>
        /// Sets the html-element to use on highlighted words
        /// <para>Defaults to "mark"</para>
        /// <para>Set to null to disable</para>
        /// </summary>
        public void SetHighlightTag(string tag) =>
            HighlightTag = tag;

        internal static List<string> Highlights { get; } = new List<string>();
    }
}