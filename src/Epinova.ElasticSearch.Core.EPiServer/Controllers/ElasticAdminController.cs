using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticAdminController : ElasticSearchControllerBase
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly ICoreIndexer _coreIndexer;
        private readonly IElasticSearchSettings _settings;
        private static Health _healthHelper;

        public ElasticAdminController(
            ILanguageBranchRepository languageBranchRepository,
            ICoreIndexer coreIndexer,
            IElasticSearchSettings settings)
        {
            _languageBranchRepository = languageBranchRepository;
            _coreIndexer = coreIndexer;
            _settings = settings;
            _healthHelper = new Health(settings);
        }

        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Index()
        {
            HealthInformation clusterHealth = _healthHelper.GetClusterHealth();
            Node[] nodeInfo = _healthHelper.GetNodeInfo();

            var indexHelper = new Index(_settings);

            var allIndices = indexHelper.GetIndices();

            ElasticSearchSection config = ElasticSearchSection.GetConfiguration();

            foreach (var index in allIndices)
            {
                var parsed = config.IndicesParsed.FirstOrDefault(i =>
                    index.Index.StartsWith(i.Name, StringComparison.InvariantCultureIgnoreCase));

                index.Type = String.IsNullOrWhiteSpace(parsed.Type)
                    ? "[default]"
                    : Type.GetType(parsed.Type).Name;
            }

            var adminViewModel = new AdminViewModel(clusterHealth, allIndices.OrderBy(i => i.Type), nodeInfo);

            return View("~/Views/ElasticSearchAdmin/Admin/Index.cshtml", adminViewModel);
        }

        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult AddNewIndex()
        {
            if (Core.Server.Info.Version.Major < 5)
                throw new Exception("Elasticsearch version 5 or higher required");

            ElasticSearchSection config = ElasticSearchSection.GetConfiguration();

            IEnumerable<string> languages = _languageBranchRepository
                .ListEnabled()
                .Select(lang => lang.LanguageID);

            foreach (string lang in languages)
            {
                foreach (IndexConfiguration indexConfig in config.IndicesParsed)
                {
                    var indexName = _settings.GetCustomIndexName(indexConfig.Name, lang);
                    Type indexType = GetIndexType(indexConfig, config);

                    var index = new Index(_settings, indexName);

                    if (!index.Exists)
                    {
                        index.Initialize(indexType);
                        index.WaitForStatus();
                        index.DisableDynamicMapping(indexType);
                        index.WaitForStatus();
                    }

                    if (IsCustomType(indexType))
                    {
                        _coreIndexer.UpdateMapping(indexType, indexType, indexName, lang, true);
                        index.WaitForStatus();
                    }
                    else if(_settings.CommerceEnabled)
                    {
                        indexName = _settings.GetCustomIndexName($"{indexConfig.Name}-{Constants.CommerceProviderName}", lang);
                        index = new Index(_settings, indexName);
                        if (!index.Exists)
                        {
                            index.Initialize(indexType);
                            index.WaitForStatus();
                            index.DisableDynamicMapping(indexType);
                            index.WaitForStatus();
                        }
                    }
                }
            }

            return RedirectToAction("Index");
        }

        private static bool IsCustomType(Type indexType)
        {
            return indexType != null && indexType != typeof(IndexItem);
        }

        private static Type GetIndexType(IndexConfiguration index, ElasticSearchSection config)
        {
            if(index.Default || config.IndicesParsed.Length == 1)
                return typeof(IndexItem);

            if (String.IsNullOrWhiteSpace(index.Type))
                return null;

            return Type.GetType(index.Type, false, true);
        }

        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult DeleteIndex(string indexName)
        {
            var indexing = new Indexing(_settings);
            indexing.DeleteIndex(indexName);

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult ChangeTokenizer(string indexName, string tokenizer)
        {
            var indexing = new Indexing(_settings);
            var index = new Index(_settings, indexName);

            indexing.Close(indexName);
            index.ChangeTokenizer(tokenizer);
            indexing.Open(indexName);

            index.WaitForStatus();

            return RedirectToAction("Index");
        }
    }
}
