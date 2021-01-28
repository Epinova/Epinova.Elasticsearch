using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Services;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticMappingController : ElasticSearchControllerBase
    {
        private readonly IElasticSearchSettings _settings;
        private readonly IMappingValidatorService _mappingValidatorService;
        private readonly IHttpClientHelper _httpClientHelper;

        public ElasticMappingController(ILanguageBranchRepository languageBranchRepository, IElasticSearchSettings settings, IMappingValidatorService mappingValidatorService, IServerInfoService serverInfoService, IHttpClientHelper httpClientHelper) : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _settings = settings;
            _mappingValidatorService = mappingValidatorService;
            _httpClientHelper = httpClientHelper;
        }

        public ActionResult Index(string index, string selectedButton)
        {
            var model = new MappingViewModel(CurrentLanguage);

            if(String.IsNullOrWhiteSpace(index))
            {
                index = CurrentIndex;
            }

            model.Indices = Indices;
            model.SelectedIndex = index;

            switch(selectedButton)
            {
                case "show":
                    model.Mappings = GetMappings(index);
                    break;
                case "validate":
                    model.Mappings = ValidateMappings(Indices.Single(i => i.Index == index));
                    break;
                default:
                    break;
            }


            model.Indices = Indices;
            model.SelectedIndex = index;
            
            return View("~/Views/ElasticSearchAdmin/Mapping/Index.cshtml", model);
        }

        private string ValidateMappings(IndexInformation index)
        {
            List<MappingValidatorType> errors = _mappingValidatorService.Validate(index);
            if(errors.Any())
                return JsonConvert.SerializeObject(errors, Formatting.Indented);

            return "No mapping errors found";
        }

        private string GetMappings(string index)
        {
            string uri = $"{_settings.Host}/{index}/_mapping";
            string response = _httpClientHelper.GetJson(new Uri(uri));

            return JToken.Parse(response).ToString(Formatting.Indented);
        }
    }
}