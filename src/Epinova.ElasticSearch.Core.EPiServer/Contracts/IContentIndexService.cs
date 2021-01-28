using System;
using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Contracts
{
    public interface IContentIndexService
    {
        Type[] ListContainedTypes(List<IContent> contentList);
        List<IContent> ListContentFromRoot(int bulkSize, ContentReference rootLink, List<LanguageBranch> languages);
        IEnumerable<IContent> ListContent(List<ContentReference> contentReferences, List<LanguageBranch> languages);
    }
}
