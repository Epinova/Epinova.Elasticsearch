using System;
using System.Collections.Generic;
using System.Globalization;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using Epinova.ElasticSearch.Core.Models.Bulk;
using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.EPiServer.Contracts
{
    public interface IIndexer
    {
        BulkBatchResult BulkUpdate(IEnumerable<IContent> contents, Action<string> logger, string index, int bulkIndex, double bulkCount, string indexingContentType);
        void Delete(ContentReference contentLink);
        void Delete(IContent content, string indexName = null);
        IndexingStatus UpdateStructure(IContent root, string indexName = null);
        IndexingStatus Update(IContent content, string indexName = null);
        CultureInfo GetLanguage(IContent content);
        bool SkipIndexing(IContent content);
        bool ShouldHideFromSearch(IContent content);
        bool IsExcludedType(IContent content);
    }
}