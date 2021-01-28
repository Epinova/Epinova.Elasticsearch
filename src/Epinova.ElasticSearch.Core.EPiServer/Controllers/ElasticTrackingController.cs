using System;
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

        public ElasticTrackingController(
            ILanguageBranchRepository languageBranchRepository,
            ITrackingRepository trackingRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper)
            : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _trackingRepository = trackingRepository;
        }

        public ActionResult Clear()
        {
            _trackingRepository.Clear(CurrentLanguage, CurrentIndex);
            return RedirectToAction("Index", new { CurrentLanguage });
        }

        public ActionResult Index() => View("~/Views/ElasticSearchAdmin/Tracking/Index.cshtml", GetModel());

        private TrackingViewModel GetModel()
        {
            var model = new TrackingViewModel(CurrentLanguage);

            foreach(var language in Languages)
            {
                var id = language.Key;
                var name = language.Value;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));

                model.AddLanguage(
                    name,
                    id,
                    UniqueIndices,
                    _trackingRepository.GetSearches(id, CurrentIndex).OrderByDescending(kvp => kvp.Searches).ToDictionary(d => d.Query, d => d.Searches),
                    _trackingRepository.GetSearchesWithoutHits(id, CurrentIndex).OrderByDescending(kvp => kvp.Searches).ToDictionary(d => d.Query, d => d.Searches)
                    );
            }

            return model;
        }
    }
}