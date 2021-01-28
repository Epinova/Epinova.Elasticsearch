using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticIndexInspectorController : ElasticSearchControllerBase
    {
        private readonly IInspectorRepository _inspectorRepository;

        public ElasticIndexInspectorController(
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper,
            IInspectorRepository inspectorRepository,
            ILanguageBranchRepository languageBranchRepository)
            : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _inspectorRepository = inspectorRepository;
        }

        public ActionResult Index(InspectViewModel model) => View("~/Views/ElasticSearchAdmin/IndexInspector/Index.cshtml", GetModel(model));

        private InspectViewModel GetModel(InspectViewModel model)
        {
            foreach(var language in Languages)
            {
                var id = language.Key;
                var name = language.Value;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));

                model.AddLanguage(
                    name,
                    id,
                    UniqueIndices);
            }

            model.NumberOfItems = new List<int> { 10, 20, 50, 100, 1000, 10000 };
            model.SelectedNumberOfItems = model.SelectedNumberOfItems > 0 ? model.SelectedNumberOfItems : model.NumberOfItems.First();
            model.SearchHits = _inspectorRepository.Search(model.SearchText, model.Analyzed, CurrentLanguage, CurrentIndex, model.SelectedNumberOfItems, model.SelectedType, CurrentIndex);
            model.TypeCounts = _inspectorRepository.GetTypes(model.SearchText, CurrentIndex);

            return model;
        }
    }
}