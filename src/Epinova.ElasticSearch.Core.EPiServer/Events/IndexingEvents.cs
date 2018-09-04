using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using EPiServer.Logging;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Events
{
    internal class IndexingEvents
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(IndexingEvents));
        private static readonly IIndexer EPiIndexer = ServiceLocator.Current.GetInstance<IIndexer>();

        internal static void DeleteFromIndex(object sender, ContentEventArgs e)
        {
            if (e?.Content == null)
                return;

            Logger.Debug("Raising event DeleteFromIndex for " + e.Content.ContentLink.ID);
            EPiIndexer.Delete(e.Content);
        }

        internal static void UpdateIndex(object sender, ContentEventArgs e)
        {
            if (ContentReference.WasteBasket.CompareToIgnoreWorkID(e.TargetLink))
            {
                DeleteFromIndex(sender, e);
                return;
            }

            Logger.Debug("Raising event UpdateIndex for " + e.Content.ContentLink.ID);

            EPiIndexer.Update(e.Content);
        }
    }
}
