using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using EPiServer.Logging;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Events
{
    internal static class IndexingEvents
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(IndexingEvents));
        private static readonly IIndexer EPiIndexer = ServiceLocator.Current.GetInstance<IIndexer>();

        internal static void DeleteFromIndex(object sender, ContentEventArgs e)
        {
            if (e?.ContentLink == null)
                return;

            Logger.Debug($"Raising event DeleteFromIndex for '{e.ContentLink}'");
            EPiIndexer.Delete(e.ContentLink);
        }

        internal static void UpdateIndex(object sender, ContentEventArgs e)
        {
            if (ContentReference.WasteBasket.CompareToIgnoreWorkID(e.TargetLink))
            {
                DeleteFromIndex(sender, e);
                return;
            }

            Logger.Debug($"Raising event UpdateIndex for '{e.Content.ContentLink}'");

            EPiIndexer.Update(e.Content);
        }
    }
}
