using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;
using EPiServer.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticConsoleController : ElasticSearchControllerBase
    {
        private readonly IElasticSearchSettings _settings;
        private readonly IServerInfoService _serverInfoService;
        private readonly IHttpClientHelper _httpClientHelper;

        public ElasticConsoleController(ILanguageBranchRepository languageBranchRepository, IElasticSearchSettings settings, IServerInfoService serverInfoService, IHttpClientHelper httpClientHelper) : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _settings = settings;
            _serverInfoService = serverInfoService;
            _httpClientHelper = httpClientHelper;
        }

        [ValidateInput(false)]
        public ActionResult Index(string query, string index)
        {
            bool runQuery = true;
            if(string.IsNullOrWhiteSpace(index))
            {
                index = _settings.GetDefaultIndexName(ContentLanguage.PreferredCulture);
                runQuery = false;
            }

            List<string> indices = Indices.Select(i => i.Index).ToList();
            
            if(String.IsNullOrWhiteSpace(query))
            {
                query = "{\n    \"size\" : 10,\n    \"query\" : {\n        \"match_all\" : {}\n    }\n}";
            }

            ConsoleViewModel model = new ConsoleViewModel(index, indices, query);
            
            if(!String.IsNullOrWhiteSpace(index) && indices.Contains(index) && runQuery)
            {
                string uri = $"{_settings.Host}/{index}/_search";
                if(_serverInfoService.GetInfo().Version >= Constants.TotalHitsAsIntAddedVersion)
                    uri += "?rest_total_hits_as_int=true";

                byte[] data = Encoding.UTF8.GetBytes(query);
                byte[] returnData = _httpClientHelper.Post(new Uri(uri), data);
                string response = Encoding.UTF8.GetString(returnData);

                model.Result = JToken.Parse(response).ToString(Formatting.Indented);
            }

            return View("~/Views/ElasticSearchAdmin/Console/Index.cshtml", model);
        }
    }
}