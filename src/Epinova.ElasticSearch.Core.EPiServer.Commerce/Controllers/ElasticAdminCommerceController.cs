using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Scheduler;
using Mediachase.Commerce.Catalog;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Controllers
{
    public class ElasticAdminCommerceController : ElasticAdminController
    {
        private readonly IElasticSearchSettings _settings;
        private readonly ReferenceConverter _referenceConverter;

        public ElasticAdminCommerceController(IContentIndexService contentIndexService, IContentTypeRepository contentTypeRepository, ILanguageBranchRepository languageBranchRepository, ICoreIndexer coreIndexer, IElasticSearchSettings settings, IHttpClientHelper httpClientHelper, IServerInfoService serverInfoService, IScheduledJobRepository scheduledJobRepository, IScheduledJobExecutor scheduledJobExecutor, ReferenceConverter referenceConverter)  : base(contentIndexService, contentTypeRepository, languageBranchRepository, coreIndexer, settings, httpClientHelper, serverInfoService, scheduledJobRepository, scheduledJobExecutor)
        {
            _settings = settings;
            _referenceConverter = referenceConverter;
        }

        public override ActionResult Index()
        {
            return View("~/Views/ElasticSearchAdmin/Admin/Index.cshtml", GetModel());
        }

        public override ActionResult AddNewIndex()
        {
            base.AddNewIndex();

            foreach(var lang in Languages)
            {
                string commerceIndexName = _settings.GetCommerceIndexName(new CultureInfo(lang.Key));
                CreateIndex(typeof(IndexItem), commerceIndexName);
            }

            return RedirectToAction("Index");
        }

        public override ActionResult AddNewIndexWithMappings()
        {
            base.AddNewIndexWithMappings();
            Type indexType = typeof(IndexItem);

            foreach(KeyValuePair<string, string> lang in Languages)
            {
                var commerceIndexName = _settings.GetCommerceIndexName(new CultureInfo(lang.Key));
                CreateIndex(indexType, commerceIndexName);

                List<Type> commerceTypes = ListCommerceContentTypes();
                UpdateMappingForTypes(commerceIndexName, lang.Key, commerceTypes);
            }

            return RedirectToAction("Index");

            List<Type> ListCommerceContentTypes()
            {
                List<Type> types = ListOptimizelyTypes();
                types.RemoveAll(t => !t.IsSubclassOf(typeof(CatalogContentBase)));
                return types;
            }
        }


        [HttpPost]
        public override ActionResult DeleteAll()
        {
            return base.DeleteAll();
        }
    }
}