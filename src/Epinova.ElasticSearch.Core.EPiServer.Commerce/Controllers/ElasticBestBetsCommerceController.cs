using System.Collections.Generic;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Commerce.Catalog.ContentTypes;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using Mediachase.Commerce.Catalog;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce.Controllers
{
    public class ElasticBestBetsCommerceController : ElasticBestBetsController
    {
        private readonly IElasticSearchSettings _settings;
        private readonly ReferenceConverter _referenceConverter;

        public ElasticBestBetsCommerceController(IContentLoader contentLoader, IBestBetsRepository bestBetsRepository, ILanguageBranchRepository languageBranchRepository, IElasticSearchSettings settings, IServerInfoService serverInfoService, IHttpClientHelper httpClientHelper, ReferenceConverter referenceConverter) : base(contentLoader, bestBetsRepository, languageBranchRepository, settings, serverInfoService, httpClientHelper)
        {
            _settings = settings;
            _referenceConverter = referenceConverter;
        }

        public override ActionResult Index()
        {
            var model = new BestBetsViewModel(CurrentLanguage)
            {
                BestBetsByLanguage = GetBestBetsByLanguage(),
                TypeName = GetTypeName(),
            };

            bool commerceSelected = _settings.GetCommerceIndexName(CurrentLanguage).Equals(CurrentIndex);
            
            if(commerceSelected)
            {
                model.SearchProviderKey = "catalog";
                model.SelectorTypes = new List<string>{ typeof(EntryContentBase).FullName.ToLower() };
                model.SelectorRoots = new List<ContentReference> { _referenceConverter.GetRootLink() };
            }
            else
            {
                model.SearchProviderKey = "pages";
            }

            return View("~/Views/ElasticSearchAdmin/BestBets/Index.cshtml", model);
        }
    }
}