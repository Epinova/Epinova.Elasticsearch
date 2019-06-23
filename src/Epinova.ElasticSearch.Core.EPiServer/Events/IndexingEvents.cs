using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAccess;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Events
{
    internal static class IndexingEvents
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(IndexingEvents));
        private static readonly IIndexer EPiIndexer = ServiceLocator.Current.GetInstance<IIndexer>();
        private static readonly IContentVersionRepository VersionRepository = ServiceLocator.Current.GetInstance<IContentVersionRepository>();
        private static readonly IContentLoader ContentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();

        internal static void DeleteFromIndex(object sender, DeleteContentEventArgs e)
        {
            Logger.Debug($"Raising event DeleteFromIndex for '{e?.ContentLink}'");

            if(ContentReference.IsNullOrEmpty(e?.ContentLink))
            {
                return;
            }

            DeleteFromIndex(e?.ContentLink);

            if(e?.DeletedDescendents != null)
            {
                foreach(var descendent in e.DeletedDescendents)
                {
                    DeleteFromIndex(descendent);
                }
            }
        }

        internal static void DeleteFromIndex(ContentReference contentLink)
        {
            if(ContentReference.IsNullOrEmpty(contentLink))
            {
                return;
            }

            EPiIndexer.Delete(contentLink);
        }

        internal static void UpdateIndex(object sender, ContentEventArgs e)
        {
            Logger.Debug($"Raising event UpdateIndex for '{e.Content?.ContentLink}'");

            // On Move, handle all descendents as well
            if(e is MoveContentEventArgs moveArgs)
            {
                HandleMoveEvent(moveArgs);
                return;
            }

            if(e is SaveContentEventArgs saveArgs)
            {
                HandleSaveEvent(saveArgs);
                return;
            }

            Logger.Debug($"Event not handled '{e.GetType()}'");
        }

        internal static void UpdateIndex(object sender, ContentSecurityEventArg e)
        {
            Logger.Debug($"ACL changed for '{e.ContentLink}'");

            var published = VersionRepository.LoadPublished(e.ContentLink);
            if(published == null)
            {
                Logger.Debug("Previously unpublished, do nothing");
                return;
            }

            if(ContentLoader.TryGet(e.ContentLink, out IContent content))
            {
                Logger.Debug("Valid content, update index");
                EPiIndexer.Update(content);
            }
        }

        private static CultureInfo GetLanguage(IContent content)
        {
            if(content is ILocale locale && locale.Language != null && !CultureInfo.InvariantCulture.Equals(locale.Language))
            {
                return locale.Language;
            }

            return CultureInfo.CurrentCulture;
        }

        private static void HandleMoveEvent(MoveContentEventArgs moveArgs)
        {
            Logger.Debug("Move-event, update index including descendents");

            var isDelete = ContentReference.WasteBasket.CompareToIgnoreWorkID(moveArgs.TargetLink);
            var language = GetLanguage(moveArgs.Content);
            var contentList = ContentLoader.GetItems(moveArgs.Descendents, language).ToList();
            contentList.Insert(0, moveArgs.Content);

            foreach(var content in contentList)
            {
                if(isDelete)
                {
                    DeleteFromIndex(content.ContentLink);
                }
                else
                {
                    EPiIndexer.Update(content);
                }
            }
        }

        private static void HandleSaveEvent(SaveContentEventArgs args)
        {
            if(IsPublishAction(args))
            {
                Logger.Debug("Publish-event, update index");
                EPiIndexer.Update(args.Content);
                return;
            }

            if(IsPublishedToCheckedOutAction(args))
            {
                Logger.Debug("Save-event, previously published, do nothing");
                return;
            }

            if(IsSaveAction(args, out ContentVersion published))
            {
                if(published == null)
                {
                    Logger.Debug("Save-event, previously unpublished, update index");
                    EPiIndexer.Update(args.Content);
                    return;
                }

                Logger.Debug("Save-event, previously published, do nothing");
                return;
            }

            // Create
            if(args.Transition.CurrentStatus == VersionStatus.NotCreated)
            {
                Logger.Debug("Create-event, do nothing");
                return;
            }

            Logger.Information($"Save-event was not handled. Action {args.Action}, transition {args.Transition.CurrentStatus}=>{args.Transition.NextStatus}");
        }

        private static bool IsSaveAction(SaveContentEventArgs args, out ContentVersion published)
        {
            if(args.Transition.CurrentStatus == VersionStatus.CheckedOut
                && args.Transition.NextStatus == VersionStatus.CheckedOut)
            {
                published = VersionRepository.LoadPublished(args.ContentLink);
                return true;
            }

            published = null;
            return false;
        }

        private static bool IsPublishedToCheckedOutAction(SaveContentEventArgs args)
        {
            return args.Transition.CurrentStatus == VersionStatus.Published
                && args.Transition.NextStatus == VersionStatus.CheckedOut;
        }

        private static bool IsPublishAction(SaveContentEventArgs args)
        {
            return args.MaskedAction == SaveAction.Publish
                && args.Transition.NextStatus == VersionStatus.Published;
        }
    }
}