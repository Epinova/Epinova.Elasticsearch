using System;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticTrackingController : ElasticSearchControllerBase
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly ITrackingRepository _trackingRepository;

        public ElasticTrackingController(ILanguageBranchRepository languageBranchRepository, ITrackingRepository trackingRepository)
        {
            _languageBranchRepository = languageBranchRepository;
            _trackingRepository = trackingRepository;
        }


        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Clear(string languageId)
        {
            _trackingRepository.Clear(languageId);
            return RedirectToAction("Index", new { languageId });
        }

        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Index(string languageId = null)
        {
            var languages = _languageBranchRepository.ListEnabled()
                .Select(lang => new {lang.LanguageID, lang.Name})
                .ToArray();

            TrackingViewModel model = new TrackingViewModel(languageId);

            foreach (var language in languages)
            {
                var name = language.Name;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));

                model.SearchesByLanguage.Add(new TrackingByLanguage
                {
                    LanguageName = name,
                    LanguageId = language.LanguageID,
                    Searches = _trackingRepository
                        .GetSearches(language.LanguageID)
                        .OrderByDescending(kvp => kvp.Searches)
                        .ToDictionary(d => d.Query, d => d.Searches)
                });

                model.SearchesWithoutHitsByLanguage.Add(new TrackingByLanguage
                {
                    LanguageName = name,
                    LanguageId = language.LanguageID,
                    Searches = _trackingRepository
                        .GetSearchesWithoutHits(language.LanguageID)
                        .OrderByDescending(kvp => kvp.Searches)
                        .ToDictionary(d => d.Query, d => d.Searches)
                });
            }

            return View("~/Views/ElasticSearchAdmin/Tracking/Index.cshtml", model);
        }
    }
}