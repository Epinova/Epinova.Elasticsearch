using System;
using System.Collections.Generic;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
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

        public ElasticAdminCommerceController(
            IContentIndexService contentIndexService,
            ILanguageBranchRepository languageBranchRepository,
            ICoreIndexer coreIndexer,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper,
            IServerInfoService serverInfoService,
            IScheduledJobRepository scheduledJobRepository,
            IScheduledJobExecutor scheduledJobExecutor,
            ReferenceConverter referenceConverter)
            : base(contentIndexService, languageBranchRepository, coreIndexer, settings, httpClientHelper, serverInfoService, scheduledJobRepository, scheduledJobExecutor)
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
                string commerceIndexName = _settings.GetCommerceIndexName(lang.Key);
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
                var commerceIndexName = _settings.GetCommerceIndexName(lang.Key);
                CreateIndex(indexType, commerceIndexName);
                
                ContentReference commerceRoot = _referenceConverter.GetRootLink();
                UpdateMappingForTypes(commerceRoot, indexType, commerceIndexName, lang.Key);
            }

            return RedirectToAction("Index");
        }

        public override ActionResult RunIndexJob()
        {
            return base.RunIndexJob();
        }
        
        public override ActionResult DeleteIndex(string indexName)
        {
            return base.DeleteIndex(indexName);
        }

        [HttpPost]
        public override ActionResult DeleteAll()
        {
            return base.DeleteAll();
        }

        public override ActionResult ChangeTokenizer(string indexName, string tokenizer)
        {
            return base.ChangeTokenizer(indexName, tokenizer);
        }
    }
}