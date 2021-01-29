using System;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    [Authorize(Roles = RoleNames.ElasticsearchEditors)]
    public class ElasticIndexerController : Controller
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ElasticIndexerController));

        private readonly IContentLoader _contentLoader;
        private readonly IIndexer _indexer;
        private readonly IElasticSearchSettings _settings;

        public ElasticIndexerController(IContentLoader contentLoader, IIndexer indexer, IElasticSearchSettings settings)
        {
            _contentLoader = contentLoader;
            _indexer = indexer;
            _settings = settings;
        }

        [HttpPost]
        public JsonResult UpdateItem(string id, bool recursive = false)
        {
            try
            {
                if(_contentLoader.TryGet(ContentReference.Parse(id), out IContent content))
                {
                    string indexName = null;

                    // Point catalog content to correct index
                    if(Constants.CommerceProviderName.Equals(content.ContentLink.ProviderName))
                    {
                        string lang = _indexer.GetLanguage(content);
                        indexName = _settings.GetCustomIndexName($"{_settings.Index}-{Constants.CommerceProviderName}", lang);
                    }

                    IndexingStatus status = recursive
                        ? _indexer.UpdateStructure(content, indexName)
                        : _indexer.Update(content, indexName);

                    return Json(new { status = status.ToString() });
                }
            }
            catch(Exception ex)
            {
                Logger.Error("Error updating item with id '" + id + "'", ex);
                return Json(new { status = nameof(IndexingStatus.Error), error = ex.Message });
            }

            return Json(new { status = nameof(IndexingStatus.Error) });
        }
    }
}