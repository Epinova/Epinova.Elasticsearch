﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Scheduler;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticAdminController : ElasticSearchControllerBase
    {
        private readonly IContentIndexService _contentIndexService;
        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly ICoreIndexer _coreIndexer;
        private readonly IElasticSearchSettings _settings;
        private readonly Health _healthHelper;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IServerInfoService _serverInfoService;
        private readonly IScheduledJobRepository _scheduledJobRepository;
        private readonly IScheduledJobExecutor _scheduledJobExecutor;

        public ElasticAdminController(
            IContentIndexService contentIndexService,
            IContentTypeRepository contentTypeRepository,
            ILanguageBranchRepository languageBranchRepository,
            ICoreIndexer coreIndexer,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper,
            IServerInfoService serverInfoService,
            IScheduledJobRepository scheduledJobRepository,
            IScheduledJobExecutor scheduledJobExecutor)
            : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _contentIndexService = contentIndexService;
            _contentTypeRepository = contentTypeRepository;
            _coreIndexer = coreIndexer;
            _settings = settings;
            _healthHelper = new Health(settings, httpClientHelper);
            _httpClientHelper = httpClientHelper;
            _serverInfoService = serverInfoService;
            _scheduledJobRepository = scheduledJobRepository;
            _scheduledJobExecutor = scheduledJobExecutor;
        }

        public virtual ActionResult Index()
        {
            if(_settings.CommerceEnabled)
                return RedirectToAction("Index", "ElasticAdminCommerce");

            return View("~/Views/ElasticSearchAdmin/Admin/Index.cshtml", GetModel());
        }

        public virtual ActionResult RunIndexJob()
        {
            var indexJob = _scheduledJobRepository.List().FirstOrDefault(job => job.Name == Constants.IndexEPiServerContentDisplayName);
            if(indexJob != null)
            {
                _scheduledJobExecutor.StartAsync(indexJob);
            }
            return RedirectToAction("Index");
        }

        public virtual ActionResult AddNewIndex()
        {
            if(_serverInfoService.GetInfo().Version < Constants.MinimumSupportedVersion)
            {
                throw new InvalidOperationException("Elasticsearch version 5 or higher required");
            }

            ElasticSearchSection config = ElasticSearchSection.GetConfiguration();

            foreach(KeyValuePair<string, string> lang in Languages)
            {
                foreach(IndexConfiguration indexConfig in config.IndicesParsed)
                {
                    Type indexType = GetIndexType(indexConfig, config);
                    CultureInfo currentCulture = new CultureInfo(lang.Key);
                    var indexName = _settings.GetCustomIndexName(indexConfig.Name, currentCulture);

                    CreateIndex(indexType, indexName);
                }
            }

            string invariantCultureIndexName = _settings.GetCustomIndexName(_settings.Index, CultureInfo.InvariantCulture);
            CreateIndex(typeof(IndexItem), invariantCultureIndexName);

            return RedirectToAction("Index");
        }
        
        public virtual ActionResult AddNewIndexWithMappings()
        {
            if(_serverInfoService.GetInfo().Version < Constants.MinimumSupportedVersion)
            {
                throw new InvalidOperationException("Elasticsearch version 5 or higher required");
            }

            ElasticSearchSection config = ElasticSearchSection.GetConfiguration();

            foreach(KeyValuePair<string, string> lang in Languages)
            {
                foreach(IndexConfiguration indexConfig in config.IndicesParsed)
                {
                    Type indexType = GetIndexType(indexConfig, config);
                    string indexName = _settings.GetCustomIndexName(indexConfig.Name, new CultureInfo(lang.Key));
                    CreateIndexAndMappings(indexName, indexType, lang.Key);
                }
            }

            string invariantCultureIndexName = _settings.GetCustomIndexName(_settings.Index, CultureInfo.InvariantCulture);
            CreateIndexAndMappings(invariantCultureIndexName, typeof(IndexItem), Constants.InvariantCultureIndexNamePostfix);

            return RedirectToAction("Index");
        }

        private void CreateIndexAndMappings(string indexName, Type indexType, string lang)
        {
            Index index = CreateIndex(indexType, indexName);

            if(IsCustomType(indexType))
                _coreIndexer.UpdateMapping(indexType, indexName, lang, optIn: false);
            else
            {
                List<Type> allTypes = ListAllTypes(lang);
                UpdateMappingForTypes(indexName, lang, allTypes);
            }

            index.WaitForStatus();
        }

        public virtual ActionResult DeleteIndex(string indexName)
        {
            var indexing = new Indexing(_serverInfoService, _settings, _httpClientHelper);
            indexing.DeleteIndex(indexName);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public virtual ActionResult DeleteAll()
        {
            var indexing = new Indexing(_serverInfoService, _settings, _httpClientHelper);

            foreach(var index in Indices)
            {
                indexing.DeleteIndex(index.Index);
            }

            return RedirectToAction("Index");
        }

        public virtual ActionResult ChangeTokenizer(string indexName, string tokenizer)
        {
            var indexing = new Indexing(_serverInfoService, _settings, _httpClientHelper);
            var index = new Index(_serverInfoService, _settings, _httpClientHelper, indexName);

            indexing.Close(indexName);
            index.ChangeTokenizer(tokenizer);
            indexing.Open(indexName);

            index.WaitForStatus();

            return RedirectToAction("Index");
        }


        protected AdminViewModel GetModel()
        {
            HealthInformation clusterHealth = _healthHelper.GetClusterHealth();
            Node[] nodeInfo = _healthHelper.GetNodeInfo();

            return new AdminViewModel(clusterHealth, allIndexes: Indices.OrderBy(i => i.TypeName), nodeInfo);
        }


        protected Index CreateIndex(Type indexType, string indexName)
        {
            var index = new Index(_serverInfoService, _settings, _httpClientHelper, indexName);
            if(!index.Exists)
            {
                index.Initialize(indexType);
                index.WaitForStatus();
            }

            return index;
        }

        private List<Type> ListCmsContentTypes()
        {
            List<Type> types = ListOptimizelyTypes();
            types.RemoveAll(t => t.Namespace == null || t.Namespace.StartsWith("EPiServer.Commerce.", StringComparison.OrdinalIgnoreCase));
            types.RemoveAll(t => !(t.IsSubclassOf(typeof(BlockData)) || t.IsSubclassOf(typeof(PageData)) || t.IsSubclassOf(typeof(BasicContent))));
            return types;
        }

        private List<Type> ListOptimizelyMediaTypes()
        {
            List<Type> types = ListOptimizelyTypes();
            types.RemoveAll(t => !t.IsSubclassOf(typeof(MediaData)));
            return types;
        }

        protected List<Type> ListOptimizelyTypes() => _contentTypeRepository.List().Select(ct => ct.ModelType).Where(t => t != null && !t.IsExcludedType()).ToList();

        protected List<Type> ListAllTypes(string languageKey) => languageKey.Equals(Constants.InvariantCultureIndexNamePostfix) ? ListOptimizelyMediaTypes() : ListCmsContentTypes();

        protected void UpdateMappingForTypes(string indexName, string languageKey, List<Type> allTypes)
        {
            foreach(Type type in allTypes)
            {
                _coreIndexer.UpdateMapping(type, indexName, languageKey, optIn: false);
            }
        }

        private static bool IsCustomType(Type indexType) => indexType != null && indexType != typeof(IndexItem);

        private static Type GetIndexType(IndexConfiguration index, ElasticSearchSection config)
        {
            if(index.Default || config.IndicesParsed.Count() == 1)
                return typeof(IndexItem);

            if(String.IsNullOrWhiteSpace(index.Type))
                return null;

            return Type.GetType(index.Type, throwOnError: false, ignoreCase: true);
        }
    }
}
