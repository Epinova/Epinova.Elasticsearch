using System;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticTrackingController : ElasticSearchControllerBase
    {
        private readonly ITrackingRepository _trackingRepository;
        private readonly IElasticSearchSettings _settings;


        public ElasticTrackingController(
            ILanguageBranchRepository languageBranchRepository,
            ITrackingRepository trackingRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper)
            : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _trackingRepository = trackingRepository;
            _settings = settings;
        }

        public ActionResult Clear(string languageID, string index)
        {
            _trackingRepository.Clear(languageID, index);
            return RedirectToAction("Index", new { languageID });
        }

        public ActionResult Index()
        {
            TrackingViewModel model = GetModel();
            return View("~/Views/ElasticSearchAdmin/Tracking/Index.cshtml", model);
        }


        private TrackingViewModel GetModel()
        {
            var model = new TrackingViewModel(CurrentLanguage);
            model.SelectedIndex = _settings.GetIndexNameWithoutLanguage(CurrentIndex);

            foreach(var language in Languages)
            {
                var languageId = language.Key;
                var name = language.Value;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));
                
                model.AddLanguage(
                    name,
                    languageId,
                    UniqueIndicesNoLanguage,
                    _trackingRepository.GetSearches(languageId, model.SelectedIndex).OrderByDescending(kvp => kvp.Searches).ToDictionary(d => d.Query, d => d.Searches),
                    _trackingRepository.GetSearchesWithoutHits(languageId, model.SelectedIndex).OrderByDescending(kvp => kvp.Searches).ToDictionary(d => d.Query, d => d.Searches)
                    );
            }

            return model;
        }
    }
}