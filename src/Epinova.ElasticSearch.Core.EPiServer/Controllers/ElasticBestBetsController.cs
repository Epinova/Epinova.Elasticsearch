using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticBestBetsController : ElasticSearchControllerBase
    {
        private readonly IBestBetsRepository _bestBetsRepository;
        private readonly IElasticSearchService _searchService;
        private readonly IElasticSearchSettings _settings;

        public ElasticBestBetsController(IBestBetsRepository bestBetsRepository, ILanguageBranchRepository languageBranchRepository, IElasticSearchService searchService, IElasticSearchSettings settings, IServerInfoService serverInfoService, IHttpClientHelper httpClientHelper) : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _bestBetsRepository = bestBetsRepository;
            _searchService = searchService;
            _settings = settings;
        }

        public virtual ActionResult Index()
        {
            var model = new BestBetsViewModel(CurrentLanguage)
            {
                BestBetsByLanguage = GetBestBetsByLanguage(),
                TypeName = GetTypeName(),
            };

            return View("~/Views/ElasticSearchAdmin/BestBets/Index.cshtml", model);
        }

        [HttpPost]
        public ActionResult Add(string phrase, int contentId, string languageId, string index, string typeName)
        {
            if(!String.IsNullOrWhiteSpace(phrase) && contentId != 0)
            {
                var language = new CultureInfo(languageId);
                var indexName = SwapLanguage(index, language);

                phrase = phrase
                .Replace("¤", String.Empty)
                .Replace("|", String.Empty);

                _bestBetsRepository.AddBestBet(language, phrase, contentId, indexName, Type.GetType(typeName));

                Indexing.SetupBestBets();
            }

            return RedirectToAction("Index", new { index, languageId });
        }

        public ActionResult Delete(string languageId, string phrase, int contentId, string index, string typeName)
        {
            if(!String.IsNullOrWhiteSpace(phrase))
            {
                var language = new CultureInfo(languageId);
                var indexName = SwapLanguage(index, language);

                _bestBetsRepository.DeleteBestBet(language, phrase, contentId, indexName, Type.GetType(typeName));
            }

            Indexing.SetupBestBets();

            return RedirectToAction("Index", new { languageId });
        }


        public List<BestBetsByLanguage> GetBestBetsByLanguage()
        {
            List<BestBetsByLanguage> list = new List<BestBetsByLanguage>();

            foreach(KeyValuePair<string, string> language in Languages)
            {
                var name = language.Value;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));
                CultureInfo currentCulture = new CultureInfo(language.Key);
                var indexName = SwapLanguage(CurrentIndex, currentCulture);
                string index = _settings.GetIndexNameWithoutLanguage(indexName);

                list.Add(new BestBetsByLanguage
                {
                    IndexName = index,
                    LanguageName = name,
                    LanguageId = language.Key,
                    Indices = UniqueIndices,
                    BestBets = GetBestBetsForLanguage(currentCulture, indexName).ToList()
                });
            }

            return list;
        }


        protected string GetTypeName()
        {
            var config = ElasticSearchSection.GetConfiguration();

            var currentType = config.IndicesParsed.FirstOrDefault(i => CurrentIndex.StartsWith(i.Name, StringComparison.InvariantCultureIgnoreCase))?.Type;
            return !String.IsNullOrEmpty(currentType)
                ? Type.GetType(currentType)?.AssemblyQualifiedName
                : typeof(IndexItem).AssemblyQualifiedName;
        }


        private List<BestBet> GetBestBetsForLanguage(CultureInfo language, string index)
        {
            List<BestBet> bestBets = _bestBetsRepository.GetBestBets(language, index).ToList();
            var bestbetIds = bestBets.Select(b => b.Id).ToArray();

            if(bestbetIds.Length > 0)
            {
                var results = _searchService.Get<Object>()
                    .UseIndex(index)
                    .InField(x => DefaultFields.Id)
                    .Filters(DefaultFields.Id, bestbetIds)
                    .GetResults();

                List<SearchHit> searchHits = results?.Hits.ToList();

                foreach(BestBet bestBet in bestBets)
                    bestBet.Name = searchHits?.SingleOrDefault(r => r.Id.Equals(bestBet.Id))?.Name;
            }

            return bestBets;
        }
    }
}