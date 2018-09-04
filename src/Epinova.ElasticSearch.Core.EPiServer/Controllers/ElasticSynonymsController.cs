using System;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer;
using EPiServer.DataAbstraction;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Settings;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticSynonymsController : ElasticSearchControllerBase
    {
        private readonly ILanguageBranchRepository _languageBranchRepository;
        private readonly ISynonymRepository _synonymRepository;
        private readonly Admin.Index _indexHelper;

        internal ElasticSynonymsController(
            IContentLoader contentLoader, 
            ISynonymRepository synonymRepository, 
            ILanguageBranchRepository languageBranchRepository,
            Admin.Index indexHelper)
        {
            _synonymRepository = synonymRepository;
            _languageBranchRepository = languageBranchRepository;
            _indexHelper = indexHelper;
        }

        public ElasticSynonymsController(
            IContentLoader contentLoader, 
            ISynonymRepository synonymRepository, 
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings) 
                : this(
                      contentLoader, 
                      synonymRepository, 
                      languageBranchRepository,
                      new Admin.Index(settings))
        {
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

            var model = new SynonymsViewModel(CurrentLanguage);

            foreach (var language in languages)
            {
                var name = language.Name;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));

                model.SynonymsByLanguage.Add(new LanguageSynonyms
                {
                    Analyzer = Language.GetLanguageAnalyzer(language.LanguageID),
                    LanguageName = name,
                    LanguageId = language.LanguageID,
                    Synonyms = _synonymRepository.GetSynonyms(language.LanguageID, index)
                        .Select(s =>
                        {
                            var key = s.From;
                            if (key.Contains("=>"))
                                key = key.Split(new[] { "=>" }, StringSplitOptions.None)[0].Trim();

                            return new Synonym { From = key, To = s.To, TwoWay = s.TwoWay};
                        })
                        .ToList()
                });
            }

            return View("~/Views/ElasticSearchAdmin/Synonyms/Index.cshtml", model);
        }

        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Delete(Synonym synonym, string languageId, string analyzer, string index)
        {
            List<Synonym> synonyms = _synonymRepository.GetSynonyms(languageId, index);
            synonyms.RemoveAll(s =>
            {
                string synonymFrom = synonym.From + (synonym.TwoWay ? null : "=>" + synonym.From);
                return s.From == synonymFrom && s.To == synonym.To && s.TwoWay == synonym.TwoWay;
            });

            _synonymRepository.SetSynonyms(languageId, analyzer, synonyms, index);

            CurrentLanguage = languageId;

            return RedirectToAction("Index", new { index, languageId });
        }

        [HttpPost]
        [Authorize(Roles = "ElasticsearchAdmins")]
        public ActionResult Add(Synonym synonym, string languageId, string analyzer, string index)
        {
            if (!String.IsNullOrWhiteSpace(synonym.From) && !String.IsNullOrWhiteSpace(synonym.To))
            {
                List<Synonym> synonyms = _synonymRepository.GetSynonyms(languageId, index);

                if (!synonym.TwoWay)
                    synonym.From += "=>" + synonym.From;

                synonyms.Add(synonym);

                _synonymRepository.SetSynonyms(languageId, analyzer, synonyms, index);
            }

            CurrentLanguage = languageId;

            return RedirectToAction("Index", new { index, languageId });
        }
    }
}
