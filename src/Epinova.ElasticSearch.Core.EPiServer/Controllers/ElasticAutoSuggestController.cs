using System;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticAutoSuggestController : ElasticSearchControllerBase
    {
        private readonly IAutoSuggestRepository _autoSuggestRepository;

        public ElasticAutoSuggestController(
            ILanguageBranchRepository languageBranchRepository,
            IAutoSuggestRepository autoSuggestRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper) : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _autoSuggestRepository = autoSuggestRepository;
        }

        public ActionResult Index(string languageId = null)
        {
            var model = new AutoSuggestViewModel(languageId);

            foreach(var language in Languages)
            {
                var name = language.Value;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));

                model.WordsByLanguage.Add(new LanguageAutoSuggestWords
                {
                    LanguageName = name,
                    LanguageId = language.Key,
                    Words = _autoSuggestRepository.GetWords(language.Key)
                });
            }

            return View("~/Views/ElasticSearchAdmin/AutoSuggest/Index.cshtml", model);
        }

        [HttpPost]
        public ActionResult AddWord(string languageId, string word)
        {
            if(!String.IsNullOrWhiteSpace(word))
            {
                _autoSuggestRepository.AddWord(languageId, word.Replace("|", String.Empty));
            }

            return RedirectToAction("Index", new { languageId });
        }

        public ActionResult DeleteWord(string languageId, string word)
        {
            if(!String.IsNullOrWhiteSpace(word))
            {
                _autoSuggestRepository.DeleteWord(languageId, word);
            }

            return RedirectToAction("Index", new { languageId });
        }
    }
}