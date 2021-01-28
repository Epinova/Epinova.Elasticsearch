using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Events;
using Epinova.ElasticSearch.Core.Models.Bulk;

namespace Epinova.ElasticSearch.Core.Contracts
{
    /// <summary>
    /// Contains hooks for indexing of content
    /// </summary>
    public interface ICoreIndexer
    {
        event OnBeforeUpdateItem BeforeUpdateItem;

        event OnAfterUpdateBestBet AfterUpdateBestBet;

        /// <summary>
        /// Removes an item from the index
        /// </summary>
        /// <param name="id">The id of item to remove</param>
        /// <param name="language">Language</param>
        /// <param name="type">Type</param>
        /// <param name="indexName">Index name</param>
        void Delete(string id, string language, Type type, string indexName = null);

        /// <summary>
        /// Adds or updates an item in the index
        /// </summary>
        void Update(string id, object objectToUpdate, string indexName, Type type = null);

        /// <summary>
        /// Updates best bets for document of type <paramref name="indexType"/>, with id <paramref name="id"/>
        /// </summary>
        void UpdateBestBets(string indexName, Type indexType, string id, string[] terms);

        void CreateAnalyzedMappingsIfNeeded(Type type, string language, string indexName = null);

        /// <summary>
        /// Removes best bets for document of type <paramref name="indexType"/>, with id <paramref name="id"/>
        /// </summary>
        void ClearBestBets(string indexName, Type indexType, string id);

        void UpdateMapping(Type type, Type indexType, string index);

        void UpdateMapping(Type type, Type indexType, string index, string language, bool optIn);

        /// <summary>
        /// Performs a bulk operation
        /// </summary>
        BulkBatchResult Bulk(IEnumerable<BulkOperation> operations, Action<string> logger);

        /// <summary>
        /// Performs a bulk operation
        /// </summary>
        BulkBatchResult Bulk(params BulkOperation[] operations);

        /// <summary>
        /// Refresh the index
        /// </summary>
        /// <remarks>https://www.elastic.co/guide/en/elasticsearch/reference/current/indices-refresh.html</remarks>
        void Refresh(string language);
    }
}