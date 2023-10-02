using System.Collections.Generic;
using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Contracts
{
    public interface IContentIndexService
    {
        IEnumerable<IContent> ListContent(List<ContentReference> contentReferences, List<LanguageBranch> languages);
    }
}
