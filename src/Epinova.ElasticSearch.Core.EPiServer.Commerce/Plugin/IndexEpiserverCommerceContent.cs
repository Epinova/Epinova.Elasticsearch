using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.EPiServer.Plugin;
using EPiServer;
using EPiServer.Core;
using EPiServer.PlugIn;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Plugin
{
    [ScheduledPlugIn(SortIndex = 100000, DisplayName = "Elasticsearch: Index Episerver Commerce contents", Description = "Indexes Episerver Commerce content in Elasticsearch.")]
    public class IndexEpiserverCommerceContent : IndexEPiServerContent
    {
        private readonly IContentLoader _contentLoader;

        protected override List<ContentReference> GetContentReferences()
        {
            OnStatusChanged("Loading all references from database...");
            return _contentLoader.GetDescendents(Constants.CatalogRootLink).ToList();
        }

        public IndexEpiserverCommerceContent()
        {
            IsStoppable = true;
            CustomIndexName = $"{Settings.Index}-{Core.Constants.CommerceProviderName}";
            _contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        }
    }
}