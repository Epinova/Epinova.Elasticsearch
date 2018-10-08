using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    [Authorize(Roles = "ElasticsearchAdmins")]
    public class ElasticBestBetsController : ElasticSearchControllerBase
    {
        private readonly IContentLoader _contentLoader;
        private readonly IBestBetsRepository _bestBetsRepository;
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly Admin.Index _indexHelper;

        internal ElasticBestBetsController(
            IContentLoader contentLoader,
            IBestBetsRepository bestBetsRepository,
            ILanguageBranchRepository languageBranchRepository,
            Admin.Index indexHelper)
        {
            _contentLoader = contentLoader;
            _bestBetsRepository = bestBetsRepository;
            _languageBranchRepository = languageBranchRepository;
            _indexHelper = indexHelper;
        }

        public ElasticBestBetsController(
            IContentLoader contentLoader,
            IBestBetsRepository bestBetsRepository,
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings)
                : this(
                      contentLoader,
                      bestBetsRepository,
                      languageBranchRepository,
                      new Admin.Index(settings))
        {
            _contentLoader = contentLoader;
            _bestBetsRepository = bestBetsRepository;
            _languageBranchRepository = languageBranchRepository;
        }


        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Index(string index = null, string languageId = null)
        {
            var languages = _languageBranchRepository.ListEnabled()
                .Select(lang => new { lang.LanguageID, lang.Name })
                .ToArray();

            var indices = _indexHelper.GetIndices()
                .Select(i => i.Index).ToList();

            if (String.IsNullOrWhiteSpace(index) || !indices.Contains(index))
                index = indices.FirstOrDefault();

            ViewBag.Indices = indices.Count > 1 ? indices : null;
            ViewBag.SelectedIndex = index;

            if (languageId != null)
                CurrentLanguage = languageId;

            var model = new BestBetsViewModel(CurrentLanguage);

            foreach (var language in languages)
            {
                var name = language.Name;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));

                model.BestBetsByLanguage.Add(new BestBetsByLanguage
                {
                    LanguageName = name,
                    LanguageId = language.LanguageID,
                    BestBets = GetBestBetsForLanguage(language.LanguageID, index)
                });
            }

            var config = ElasticSearchSection.GetConfiguration();

            foreach (ContentSelectorConfiguration entry in config.ContentSelector)
            {
                model.SelectorTypes.Add(entry.Type.ToLower());
                model.SelectorRoots.Add(new ContentReference(entry.Id, entry.Provider));
            }

            var currentType = config.IndicesParsed.FirstOrDefault(i => index.StartsWith(i.Name, StringComparison.InvariantCultureIgnoreCase))?.Type;
            if (!String.IsNullOrEmpty(currentType))
                ViewBag.TypeName = Type.GetType(currentType).AssemblyQualifiedName;
            else
                ViewBag.TypeName = typeof(IndexItem).AssemblyQualifiedName;

            return View("~/Views/ElasticSearchAdmin/BestBets/Index.cshtml", model);
        }

        private IEnumerable<BestBet> GetBestBetsForLanguage(string language, string index)
        {
            var bestBets = _bestBetsRepository.GetBestBets(language, index);

            foreach (BestBet bestBet in bestBets)
            {
                var contentLink = new ContentReference(Convert.ToInt32(bestBet.Id), bestBet.Provider);
                if (_contentLoader.TryGet(contentLink, out IContent content))
                    bestBet.Name = content.Name;
                yield return bestBet;
            }
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

            CurrentLanguage = languageId;

            return RedirectToAction("Index", new { index, languageId });
        }

        public ActionResult Delete(string languageId, string phrase, string contentId, string index, string typeName)
        {
            if (!String.IsNullOrWhiteSpace(phrase))
                _bestBetsRepository.DeleteBestBet(languageId, phrase, contentId, index, Type.GetType(typeName));

            Indexing.SetupBestBets();

            CurrentLanguage = languageId;

            return RedirectToAction("Index");
        }
    }
}
