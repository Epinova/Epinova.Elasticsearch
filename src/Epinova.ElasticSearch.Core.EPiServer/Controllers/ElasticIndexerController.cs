using System;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticIndexerController : Controller
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ElasticIndexerController));

        private readonly IContentLoader _contentLoader;
        private readonly IIndexer _indexer;

        public ElasticIndexerController(IContentLoader contentLoader, IIndexer indexer)
        {
            _contentLoader = contentLoader;
            _indexer = indexer;
        }


        [HttpPost]
        [Authorize(Roles = "WebEditors,WebAdmins,Administrators")]
        public JsonResult UpdateItem(string id, bool recursive = false)
        {
            try
            {
                if (_contentLoader.TryGet(ContentReference.Parse(id), out IContent content))
                {
                    IndexingStatus status = recursive
                        ? _indexer.UpdateStructure(content)
                        : _indexer.Update(content);

                    return Json(new { status = status.ToString() });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error updating item with id '" + id + "'", ex);
                return Json(new { status = IndexingStatus.Error.ToString(), error = ex.Message });
            }

            return Json(new { status = IndexingStatus.Error.ToString() });
        }
    }
}