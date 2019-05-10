using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Events;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(IndexInitializer))]
    public class IndexingInitializer : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            Indexing.Instance.ExcludeType<SynonymBackupFile>();
            Indexing.Instance.ExcludeType<SynonymBackupFileFolder>();

            IContentEvents events = ServiceLocator.Current.GetInstance<IContentEvents>();
            events.PublishedContent += IndexingEvents.UpdateIndex;
            events.DeletingContent += IndexingEvents.DeleteFromIndex;
            events.MovedContent += IndexingEvents.UpdateIndex;

            //INFO: Might be useful later for re-indexing upon ACL changes
            //IContentSecurityRepository contentSecurityRepository = ServiceLocator.Current.GetInstance<IContentSecurityRepository>();
            //contentSecurityRepository.ContentSecuritySaved += ...
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
