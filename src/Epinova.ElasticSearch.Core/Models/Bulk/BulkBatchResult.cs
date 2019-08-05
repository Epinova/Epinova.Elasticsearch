using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public sealed class BulkBatchResult
    {
        public List<BulkResult> Batches { get; } = new List<BulkResult>();
    }
}