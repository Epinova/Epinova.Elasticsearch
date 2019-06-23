using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public sealed class BulkBatchResult
    {
        public BulkBatchResult()
        {
            Batches = new List<BulkResult>();
        }

        public List<BulkResult> Batches { get; }
    }
}