using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticBoostingController : ElasticSearchControllerBase
    {
        private readonly IBoostingRepository _boostingRepository;
        private readonly IContentTypeRepository _pageTypeRepository;

        public ElasticBoostingController(
            ILanguageBranchRepository languageBranchRepository,
            IBoostingRepository boostingRepository,
            IContentTypeRepository pageTypeRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper)
            : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _boostingRepository = boostingRepository;
            _pageTypeRepository = pageTypeRepository;
        }

        public ActionResult Index()
        {
            BoostingViewModel model = new BoostingViewModel();
            var pageTypes = _pageTypeRepository.List()
                .Where(p => p.ModelType != null)
                .OrderBy(p => p.LocalizedName);

            foreach(var type in pageTypes)
            {
                var currentBoosting = _boostingRepository.GetByType(type.ModelType);
                var indexableProps = type.ModelType
                    .GetIndexableProps(false)
                    .Select(p => p.Name);

                var propsWithBoost = type.PropertyDefinitions
                    .Where(p => indexableProps.Contains(p.Name))
                    .OrderBy(p => p.Tab.Name)
                    .ThenBy(p => p.TranslateDisplayName())
                    .Select(p => new BoostItem
                    {
                        TypeName = p.Name,
                        DisplayName = p.TranslateDisplayName(),
                        GroupName = p.Tab.Name,
                        Weight = 1
                    })
                    .ToList();

                foreach(var boost in currentBoosting)
                {
                    if(propsWithBoost.Any(p => p.TypeName == boost.Key))
                    {
                        propsWithBoost.First(p => p.TypeName == boost.Key).Weight = boost.Value;
                    }
                }

                if(propsWithBoost.Count > 0)
                {
                    model.BoostingByType.Add(type.ModelType.GetTypeName(), propsWithBoost);
                }
            }

            return View("~/Views/ElasticSearchAdmin/Boosting/Index.cshtml", model);
        }

        [HttpPost]
        public ActionResult Save(BoostingInputModel input)
        {
            input.Boosting = input.Boosting.Where(i => i.Value > 1).ToDictionary(x => x.Key, x => x.Value);
            _boostingRepository.Save(input.TypeName, input.Boosting);

            return RedirectToAction("Index");
        }
    }
}
