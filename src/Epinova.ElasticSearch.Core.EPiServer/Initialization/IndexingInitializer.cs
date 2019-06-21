using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Events;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

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

            IContentEvents events = context.Locate.Advanced.GetInstance<IContentEvents>();

            events.PublishedContent += IndexingEvents.UpdateIndex;
            events.DeletingContent += IndexingEvents.DeleteFromIndex;
            events.MovedContent += IndexingEvents.UpdateIndex;
            events.SavedContent += IndexingEvents.UpdateIndex;

            IContentSecurityRepository contentSecurityRepository = context.Locate.Advanced.GetInstance<IContentSecurityRepository>();
            contentSecurityRepository.ContentSecuritySaved += IndexingEvents.UpdateIndex;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
