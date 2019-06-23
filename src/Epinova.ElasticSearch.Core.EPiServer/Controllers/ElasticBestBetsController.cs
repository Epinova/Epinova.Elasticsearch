using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
    public class ElasticBestBetsController : ElasticSearchControllerBase
    {
        private readonly IContentLoader _contentLoader;
        private readonly IBestBetsRepository _bestBetsRepository;

        public ElasticBestBetsController(
            IContentLoader contentLoader,
            IBestBetsRepository bestBetsRepository,
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings) : base(settings, languageBranchRepository)
        {
            _contentLoader = contentLoader;
            _bestBetsRepository = bestBetsRepository;
        }

        [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
        public ActionResult Index()
        {
            var model = new BestBetsViewModel(CurrentLanguage);

            foreach (var language in Languages)
            {
                var name = language.Value;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));
                var indexName = SwapLanguage(CurrentIndex, language.Key);

                model.BestBetsByLanguage.Add(new BestBetsByLanguage
                {
                    LanguageName = name,
                    LanguageId = language.Key,
                    Indices = UniqueIndices,
                    BestBets = GetBestBetsForLanguage(language.Key, indexName)
                });
            }

            var config = ElasticSearchSection.GetConfiguration();

            foreach (ContentSelectorConfiguration entry in config.ContentSelector)
            {
                model.SelectorTypes.Add(entry.Type.ToLower());
                model.SelectorRoots.Add(new ContentReference(entry.Id, entry.Provider));
            }

            var currentType = config.IndicesParsed.FirstOrDefault(i => CurrentIndex.StartsWith(i.Name, StringComparison.InvariantCultureIgnoreCase))?.Type;
            if (!String.IsNullOrEmpty(currentType))
            {
                ViewBag.TypeName = Type.GetType(currentType).AssemblyQualifiedName;
            }
            else
            {
                ViewBag.TypeName = typeof(IndexItem).AssemblyQualifiedName;
            }

            return View("~/Views/ElasticSearchAdmin/BestBets/Index.cshtml", model);
        }

        [HttpPost]
        public ActionResult Add(string phrase, ContentReference contentId, string languageId, string index, string typeName)
        {
            if (!String.IsNullOrWhiteSpace(phrase) && !ContentReference.IsNullOrEmpty(contentId))
            {
                phrase = phrase
                    .Replace("¤", String.Empty)
                    .Replace("|", String.Empty);

                _bestBetsRepository.AddBestBet(languageId, phrase, contentId, index, Type.GetType(typeName));

                Indexing.SetupBestBets();
            }

            return RedirectToAction("Index", new { index, languageId });
        }

        public ActionResult Delete(string languageId, string phrase, string contentId, string index, string typeName)
        {
            if (!String.IsNullOrWhiteSpace(phrase))
            {
                _bestBetsRepository.DeleteBestBet(languageId, phrase, contentId, index, Type.GetType(typeName));
            }

            Indexing.SetupBestBets();

            return RedirectToAction("Index", new { languageId });
        }

        private IEnumerable<BestBet> GetBestBetsForLanguage(string language, string index)
        {
            foreach (BestBet bestBet in _bestBetsRepository.GetBestBets(language, index))
            {
                var contentLink = new ContentReference(Convert.ToInt32(ContentReference.Parse(bestBet.Id).ID), bestBet.Provider);
                if (_contentLoader.TryGet(contentLink, out IContent content))
                {
                    bestBet.Name = content.Name;
                }

                yield return bestBet;
            }
        }
    }
}
