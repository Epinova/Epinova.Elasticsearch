using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.DataAbstraction;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticSynonymsController : ElasticSearchControllerBase
    {
        private readonly ISynonymRepository _synonymRepository;

        public ElasticSynonymsController(
            ILanguageBranchRepository languageBranchRepository,
            ISynonymRepository synonymRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper)
            : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _synonymRepository = synonymRepository;
        }

        public ActionResult Index() => View("~/Views/ElasticSearchAdmin/Synonyms/Index.cshtml", GetModel());

        public ActionResult Delete(Synonym synonym, string languageId, string analyzer, string index)
        {
            List<Synonym> synonyms = _synonymRepository.GetSynonyms(languageId, index);
            synonyms.RemoveAll(s =>
            {
                string synonymFrom = String.Join(",", synonym.From
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w.Trim()));

                if(!s.TwoWay && !s.MultiWord)
                {
                    synonymFrom += "=>" + synonymFrom;
                }

                return s.From == synonymFrom && s.To == synonym.To && s.TwoWay == synonym.TwoWay && s.MultiWord == synonym.MultiWord;
            });

            _synonymRepository.SetSynonyms(languageId, analyzer, synonyms, index);

            return RedirectToAction("Index", new { index, languageId });
        }

        [HttpPost]
        public ActionResult Add(Synonym synonym, string languageId, string analyzer, string index)
        {
            if(!String.IsNullOrWhiteSpace(synonym.From) && !String.IsNullOrWhiteSpace(synonym.To))
            {
                List<Synonym> synonyms = _synonymRepository.GetSynonyms(languageId, index);

                var fromWords = synonym.From
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .ToArray();

                if(fromWords.Length > 1)
                {
                    synonym.MultiWord = true;
                    synonym.From = String.Join(",", fromWords);
                }
                else if(!synonym.TwoWay)
                {
                    synonym.From += "=>" + synonym.From;
                }

                synonym.From = synonym.From.ToLower();
                synonym.To = synonym.To.ToLower();

                synonyms.Add(synonym);

                _synonymRepository.SetSynonyms(languageId, analyzer, synonyms, index);
            }

            return RedirectToAction("Index", new { index, languageId });
        }

        private SynonymsViewModel GetModel()
        {
            var model = new SynonymsViewModel(CurrentLanguage);

            foreach(var language in Languages)
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
                            if(key.Contains("=>"))
                            {
                                key = key.Split(new[] { "=>" }, StringSplitOptions.None)[0].Trim();
                            }

                            var fromDisplay = String.Join(", ", key.Split(','));
                            return new Synonym { From = fromDisplay, To = s.To, TwoWay = s.TwoWay, MultiWord = s.MultiWord };
                        })
                        .ToList()
                });
            }

            return model;
        }
    }
}
