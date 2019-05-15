using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using EPiServer.Logging;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Events
{
    internal static class IndexingEvents
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(IndexingEvents));
        private static readonly IIndexer EPiIndexer = ServiceLocator.Current.GetInstance<IIndexer>();
        private static readonly IContentVersionRepository VersionRepository = ServiceLocator.Current.GetInstance<IContentVersionRepository>();

        internal static void DeleteFromIndex(object sender, ContentEventArgs e)
        {
            if (e?.ContentLink == null)
                return;

            Logger.Debug($"Raising event DeleteFromIndex for '{e.ContentLink}'");
            EPiIndexer.Delete(e.ContentLink);
        }

        internal static void UpdateIndex(object sender, ContentEventArgs e)
        {
            Logger.Debug($"Raising event UpdateIndex for '{e.Content?.ContentLink}'");

            if (ContentReference.WasteBasket.CompareToIgnoreWorkID(e.TargetLink))
            {
                DeleteFromIndex(sender, e);
                return;
            }

            var saveArgs = e as SaveContentEventArgs;
            if (saveArgs == null)
                return;

            // Publish
            if (saveArgs.MaskedAction == SaveAction.Publish && saveArgs.Transition.NextStatus == VersionStatus.Published)
            {
                Logger.Debug("Publish-event, update index");
                EPiIndexer.Update(e.Content);
                return;
            }

            // Published => CheckedOut
            if (saveArgs.Transition.CurrentStatus == VersionStatus.Published && saveArgs.Transition.NextStatus == VersionStatus.CheckedOut)
            {
                Logger.Debug("Save-event, previously published, do nothing");
                return;
            }

            // CheckedOut => CheckedOut
            if (saveArgs.Transition.CurrentStatus == VersionStatus.CheckedOut && saveArgs.Transition.NextStatus == VersionStatus.CheckedOut)
            {
                var published = VersionRepository.LoadPublished(e.ContentLink);
                if (published == null)
                {
                    Logger.Debug("Save-event, previously unpublished, update index");
                    EPiIndexer.Update(e.Content);
                    return;
                }

                Logger.Debug("Save-event, previously published, do nothing");
                return;
            }

            // Create
            if (saveArgs.Transition.CurrentStatus == VersionStatus.NotCreated)
            {
                Logger.Debug("Create-event, do nothing");
                return;
            }
        }
    }
}
