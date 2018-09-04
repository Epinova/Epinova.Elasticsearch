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
        private readonly IElasticSearchSettings _elasticSearchSettings;
        private readonly IInspectorRepository _inspectorRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public ElasticIndexInspectorController(IElasticSearchSettings elasticSearchSettings, IInspectorRepository inspectorRepository, ILanguageBranchRepository languageBranchRepository)
        {
            _elasticSearchSettings = elasticSearchSettings;
            _inspectorRepository = inspectorRepository;
            _languageBranchRepository = languageBranchRepository;
        }


        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Index(InspectViewModel model)
        {
            model.Languages = _languageBranchRepository.ListEnabled();
            model.SelectedLanguage = model.SelectedLanguage ?? model.Languages.First().LanguageID;
            model.Indices = _elasticSearchSettings.Indices.ToList();
            model.SelectedIndex = model.SelectedIndex ?? _elasticSearchSettings.Index;
            model.NumberOfItems = new List<int> { 10, 20, 50, 100, 1000, 10000 };
            model.SelectedNumberOfItems = model.SelectedNumberOfItems > 0 ? model.SelectedNumberOfItems : model.NumberOfItems.First();

            model.SearchHits = _inspectorRepository.Search(model.SelectedLanguage, model.SearchText, model.SelectedNumberOfItems, model.SelectedType, model.SelectedIndex);
            model.TypeCounts = _inspectorRepository.GetTypes(model.SelectedLanguage, model.SearchText, model.SelectedIndex);

            return View("~/Views/ElasticSearchAdmin/IndexInspector/Index.cshtml", model);
        }
    }
}