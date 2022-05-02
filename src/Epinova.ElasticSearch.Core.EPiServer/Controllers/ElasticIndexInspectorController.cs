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
        private readonly IElasticSearchSettings _settings;
        private readonly IInspectorRepository _inspectorRepository;

        public ElasticIndexInspectorController(IElasticSearchSettings settings, IServerInfoService serverInfoService, IHttpClientHelper httpClientHelper, IInspectorRepository inspectorRepository, ILanguageBranchRepository languageBranchRepository) : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _settings = settings;
            _inspectorRepository = inspectorRepository;
        }

        public ActionResult Index(InspectViewModel model, string index)
        {
            model.SelectedIndex = string.IsNullOrWhiteSpace(index)
                ? _settings.GetDefaultIndexName(new System.Globalization.CultureInfo(CurrentLanguage))
                : index;

            model.Indices = Indices.Select(i => new KeyValuePair<string,string>(i.Index, $"{i.DisplayName} ({_settings.GetLanguageFromIndexName(i.Index)})")).ToList();
            model.NumberOfItems = new List<int> { 10, 20, 50, 100, 1000, 10000 };
            model.SelectedNumberOfItems = model.SelectedNumberOfItems > 0 ? model.SelectedNumberOfItems : model.NumberOfItems.First();
            model.SearchHits = _inspectorRepository.Search(model.SearchText, model.Analyzed, model.SelectedIndex, model.SelectedNumberOfItems, model.SelectedType);
            model.TypeCounts = _inspectorRepository.GetTypes(model.SearchText, model.SelectedIndex);
            model.SelectedIndexName = model.Indices.FirstOrDefault(i => i.Key.Equals(model.SelectedIndex, StringComparison.OrdinalIgnoreCase)).Value;

            return View("~/Views/ElasticSearchAdmin/IndexInspector/Index.cshtml", model);
        }



    }
}