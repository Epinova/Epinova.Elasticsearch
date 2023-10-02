using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Services
{
    [ServiceConfiguration(ServiceType = typeof(IContentIndexService), Lifecycle = ServiceInstanceScope.Transient)]
    public class ContentIndexService : IContentIndexService
    {
        private readonly IContentLoader _contentLoader;
        
        public ContentIndexService(IContentLoader contentLoader)
        {
            _contentLoader = contentLoader;
        }
        
        public IEnumerable<IContent> ListContent(List<ContentReference> contentReferences, List<LanguageBranch> languages)
        {
            return languages.Any()
                ? languages.SelectMany(l => _contentLoader.GetItems(contentReferences, l.Culture))
                : _contentLoader.GetItems(contentReferences, CultureInfo.InvariantCulture);
        }
    }
}
