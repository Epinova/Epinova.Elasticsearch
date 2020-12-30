using System;
using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Services
{
    public class ContentIndexService : IContentIndexService
    {
        private readonly IContentLoader _contentLoader;
        private readonly IIndexer _indexer;
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public ContentIndexService(IContentLoader contentLoader, IIndexer indexer, ILanguageBranchRepository languageBranchRepository)
        {
            _contentLoader = contentLoader;
            _indexer = indexer;
            _languageBranchRepository = languageBranchRepository;
        }

        public Type[] GetAllTypes(List<IContent> contentList)
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

        public List<IContent> GetAllContents(int bulkSize, ContentReference rootLink, List<LanguageBranch> languages)
        {
            List<ContentReference> contentReferences = GetContentReferences(rootLink);

            List<IContent> contentList = new List<IContent>();

            while(contentReferences.Count > 0)
            {
                List<IContent> bulkContents = GetDescendentContents(contentReferences.Take(bulkSize).ToList(), languages);

                bulkContents.RemoveAll(_indexer.SkipIndexing);
                bulkContents.RemoveAll(_indexer.IsExcludedType);

                contentList.AddRange(bulkContents);
                var removeCount = contentReferences.Count >= bulkSize ? bulkSize : contentReferences.Count;
                contentReferences.RemoveRange(0, removeCount);
            }

            return contentList;
        }

        public List<ContentReference> GetContentReferences(ContentReference rootLink)
        {
            return _contentLoader.GetDescendents(rootLink).ToList();
        }

        public List<IContent> GetDescendentContents(List<ContentReference> contentReferences, List<LanguageBranch> languages)
        {
            var contentItems = new List<IContent>();

            foreach(LanguageBranch languageBranch in languages)
            {
                contentItems.AddRange(_contentLoader.GetItems(contentReferences, languageBranch.Culture));
            }

            return contentItems;
        }
    }
}
