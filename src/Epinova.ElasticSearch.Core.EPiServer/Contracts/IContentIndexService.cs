using System;
using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Contracts
{
    public interface IContentIndexService
    {
        Type[] GetAllTypes(List<IContent> contentList);
        List<IContent> GetAllContents(int bulkSize, ContentReference rootLink, List<LanguageBranch> languages);
        List<ContentReference> GetContentReferences(ContentReference rootLink);
        List<IContent> GetDescendentContents(List<ContentReference> contentReferences, List<LanguageBranch> languages);
    }
}
