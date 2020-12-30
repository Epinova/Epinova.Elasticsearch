using System;
using System.Collections.Generic;
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
        private readonly IIndexer _indexer;

        public ContentIndexService(IContentLoader contentLoader, IIndexer indexer)
        {
            _contentLoader = contentLoader;
            _indexer = indexer;
        }

        public Type[] ListContainedTypes(List<IContent> contentList)
        {
            Type[] uniqueTypes = contentList.Select(content =>
                {
                    var type = content.GetType();
                    return type.Name.EndsWith("Proxy") ? type.BaseType : type;
                })
                .Distinct()
                .ToArray();

            return uniqueTypes;
        }

        public List<IContent> ListContentFromRoot(int bulkSize, ContentReference rootLink, List<LanguageBranch> languages)
        {
            List<ContentReference> contentReferences = _contentLoader.GetDescendents(rootLink).ToList();

            List<IContent> contentList = new List<IContent>();

            while(contentReferences.Count > 0)
            {
                List<IContent> bulkContents = ListContent(contentReferences.Take(bulkSize).ToList(), languages).ToList();

                bulkContents.RemoveAll(_indexer.SkipIndexing);
                bulkContents.RemoveAll(_indexer.IsExcludedType);

                contentList.AddRange(bulkContents);
                var removeCount = contentReferences.Count >= bulkSize ? bulkSize : contentReferences.Count;
                contentReferences.RemoveRange(0, removeCount);
            }

            return contentList;
        }

        public IEnumerable<IContent> ListContent(List<ContentReference> contentReferences, List<LanguageBranch> languages) => languages.SelectMany(l => _contentLoader.GetItems(contentReferences, l.Culture));
    }
}
