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
        }

        static Indexing()
        {
            SetupBestBets();
        }

        /// <summary>
        /// The singleton instance property
        /// </summary>
        public static Indexing Instance { get; } = new Indexing();
    }
}