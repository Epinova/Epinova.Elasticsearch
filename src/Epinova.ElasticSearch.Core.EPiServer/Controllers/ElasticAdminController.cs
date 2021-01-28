using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
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
        private readonly ICoreIndexer _coreIndexer;
        private readonly IElasticSearchSettings _settings;
        private readonly Health _healthHelper;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IServerInfoService _serverInfoService;
        private readonly IScheduledJobRepository _scheduledJobRepository;
        private readonly IScheduledJobExecutor _scheduledJobExecutor;

        public ElasticAdminController(
            IContentIndexService contentIndexService,
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
                    var indexName = _settings.GetCustomIndexName(indexConfig.Name, lang.Key);

                    CreateIndex(indexType, indexName);
                }
            }

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
                    var indexName = _settings.GetCustomIndexName(indexConfig.Name, lang.Key);

                    Index index = CreateIndex(indexType, indexName);

                    if(IsCustomType(indexType))
                    {
                        _coreIndexer.UpdateMapping(indexType, indexType, indexName, lang.Key, optIn: false);
                    }
                    else
                    {
                        UpdateMappingForTypes(ContentReference.RootPage, indexType, indexName, lang.Key);
                    }

                    index.WaitForStatus();
                }
            }

            return RedirectToAction("Index");
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

            return new AdminViewModel(clusterHealth, allIndexes: Indices.OrderBy(i => i.Type), nodeInfo);
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

        protected void UpdateMappingForTypes(ContentReference rootLink, Type indexType, string indexName, string languageKey)
        {
            List<IContent> allContents = _contentIndexService.ListContentFromRoot(_settings.BulkSize, rootLink, new List<LanguageBranch> { new LanguageBranch(languageKey) });
            Type[] types = _contentIndexService.ListContainedTypes(allContents);

            foreach(Type type in types)
            {
                _coreIndexer.UpdateMapping(type, indexType, indexName, languageKey, optIn: false);
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
