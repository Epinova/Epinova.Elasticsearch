using System;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.DataAbstraction;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Settings;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticSynonymsController : ElasticSearchControllerBase
    {
        private readonly ISynonymRepository _synonymRepository;

        public ElasticSynonymsController(
            ILanguageBranchRepository languageBranchRepository,
            ISynonymRepository synonymRepository,
            IElasticSearchSettings settings) : base(settings, languageBranchRepository)
        {
            _synonymRepository = synonymRepository;
        }

        [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
        public ActionResult Index()
        {
            return View("~/Views/ElasticSearchAdmin/Synonyms/Index.cshtml", GetModel());
        }

        [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
        public ActionResult Delete(Synonym synonym, string languageId, string analyzer, string index)
        {
            List<Synonym> synonyms = _synonymRepository.GetSynonyms(languageId, index);
            synonyms.RemoveAll(s =>
            {
                string synonymFrom = synonym.From + (synonym.TwoWay ? null : "=>" + synonym.From);
                return s.From == synonymFrom && s.To == synonym.To && s.TwoWay == synonym.TwoWay;
            });

            _synonymRepository.SetSynonyms(languageId, analyzer, synonyms, index);

            return RedirectToAction("Index", new { index, languageId });
        }

        [HttpPost]
        [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
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

            return RedirectToAction("Index", new { index, languageId });
        }

        private SynonymsViewModel GetModel()
        {
            var model = new SynonymsViewModel(CurrentLanguage);

            foreach (var language in Languages)
            {
                var name = language.Value;
                name = String.Concat(name.Substring(0, 1).ToUpper(), name.Substring(1));
                var indexName = SwapLanguage(CurrentIndex, language.Key);

                model.SynonymsByLanguage.Add(new LanguageSynonyms
                {
                    Analyzer = Language.GetLanguageAnalyzer(language.Key),
                    LanguageName = name,
                    LanguageId = language.Key,
                    IndexName = indexName,
                    Indices = UniqueIndices,
                    HasSynonymsFile = !String.IsNullOrWhiteSpace(_synonymRepository.GetSynonymsFilePath(language.Key, indexName)),
                    Synonyms = _synonymRepository.GetSynonyms(language.Key, CurrentIndex)
                        .Select(s =>
                        {
                            var key = s.From;
                            if (key.Contains("=>"))
                                key = key.Split(new[] { "=>" }, StringSplitOptions.None)[0].Trim();

                            var fromDisplay = String.Join(", ", key.Split(','));
                            return new Synonym { From = fromDisplay, To = s.To, TwoWay = s.TwoWay };
                        })
                        .ToList()
                });
            }

            return model;
        }
    }
}
