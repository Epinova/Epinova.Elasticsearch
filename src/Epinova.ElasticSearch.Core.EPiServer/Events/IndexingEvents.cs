using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using EPiServer.Logging;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Events
{
    internal static class IndexingEvents
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(IndexingEvents));
        private static readonly IIndexer EPiIndexer = ServiceLocator.Current.GetInstance<IIndexer>();
        private static readonly IContentVersionRepository VersionRepository = ServiceLocator.Current.GetInstance<IContentVersionRepository>();
        private static readonly IContentLoader ContentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

        internal static void UpdateIndex(object sender, ContentSecurityEventArg e)
        {
            Logger.Debug($"ACL changed for '{e.ContentLink}'");

            var published = VersionRepository.LoadPublished(e.ContentLink);
            if (published == null)
            {
                Logger.Debug("Previously unpublished, do nothing");
                return;
            }

            if (ContentLoader.TryGet(e.ContentLink, out IContent content))
            {
                Logger.Debug("Valid content, update index");
                EPiIndexer.Update(content);
            }
        }

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
