using System;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticTrackingController : ElasticSearchControllerBase
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly ITrackingRepository _trackingRepository;
        private readonly Admin.Index _indexHelper;

        internal ElasticTrackingController(
            IContentLoader contentLoader, 
            ILanguageBranchRepository languageBranchRepository, 
            ITrackingRepository trackingRepository,
            Admin.Index indexHelper)
        {
            _languageBranchRepository = languageBranchRepository;
            _trackingRepository = trackingRepository;
            _indexHelper = indexHelper;
        }

        public ElasticTrackingController(
            IContentLoader contentLoader,
            ILanguageBranchRepository languageBranchRepository,
            ITrackingRepository trackingRepository,
            IElasticSearchSettings settings)
            : this(
                contentLoader,
                languageBranchRepository,
                trackingRepository,
                new Admin.Index(settings))
        {
        }


        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Clear(string languageId, string index)
        {
            _trackingRepository.Clear(languageId, index);
            CurrentLanguage = languageId;
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Index(string index = null)
        {
            var languages = _languageBranchRepository.ListEnabled()
                .Select(lang => new {lang.LanguageID, lang.Name})
                .ToArray();

            var indices = _indexHelper.GetIndices()
                .Select(i => i.Index).ToList();

            if (String.IsNullOrWhiteSpace(index) || !indices.Contains(index))
                index = indices.FirstOrDefault();

            ViewBag.Indices = indices.Count > 1 ? indices : null;
            ViewBag.SelectedIndex = index;
            
            TrackingViewModel model = new TrackingViewModel(CurrentLanguage);

            foreach (var language in languages)
            {
                var name = language.Name;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));

                model.SearchesByLanguage.Add(new TrackingByLanguage
                {
                    LanguageName = name,
                    LanguageId = language.LanguageID,
                    Searches = _trackingRepository
                        .GetSearches(language.LanguageID, index)
                        .OrderByDescending(kvp => kvp.Searches)
                        .ToDictionary(d => d.Query, d => d.Searches)
                });

                model.SearchesWithoutHitsByLanguage.Add(new TrackingByLanguage
                {
                    LanguageName = name,
                    LanguageId = language.LanguageID,
                    Searches = _trackingRepository
                        .GetSearchesWithoutHits(language.LanguageID, index)
                        .OrderByDescending(kvp => kvp.Searches)
                        .ToDictionary(d => d.Query, d => d.Searches)
                });
            }

            return View("~/Views/ElasticSearchAdmin/Tracking/Index.cshtml", model);
        }
    }
}