using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Conventions
{
    /// <summary>
    /// Contains methods for configuring custom conventions for the search. 
    /// Should only be run once at application start.
    /// </summary>
    public sealed partial class Indexing
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Indexing));

        private Indexing()
        {
            SearchableProperties = new ConcurrentDictionary<Type, string[]>();
            HighlightFragmentSize = 150;
            HighlightTag = "mark";
        }

        static Indexing()
        {
            CustomProperties = new List<CustomProperty>();
            SuggestionList = new List<Suggestion>();

            SetupBestBets();
        }

        /// <summary>
        /// The singleton instance property
        /// </summary>
        public static Indexing Instance { get; } = new Indexing();
    }
}