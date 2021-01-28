using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticConsoleController : ElasticSearchControllerBase
    {
        private readonly IElasticSearchSettings _settings;
        private readonly IServerInfoService _serverInfoService;
        private readonly IHttpClientHelper _httpClientHelper;

        public ElasticConsoleController(
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper)
            : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _settings = settings;
            _serverInfoService = serverInfoService;
            _httpClientHelper = httpClientHelper;
        }

        [ValidateInput(false)]
        public ActionResult Index(string query, string index)
        {
            if(String.IsNullOrWhiteSpace(index))
            {
                index = CurrentIndex;
            }

            var indexHelper = new Admin.Index(_serverInfoService, _settings, _httpClientHelper, index);

            List<string> indices = indexHelper.GetIndices()
                .Select(i => i.Index).ToList();

            if(!indices.Contains(index))
            {
                index = CurrentIndex;
            }

            ViewBag.Indices = indices;
            ViewBag.SelectedIndex = index;

            if(String.IsNullOrWhiteSpace(query))
            {
                query = "{\n    \"size\" : 10,\n    \"query\" : {\n        \"match_all\" : {}\n    }\n}";
            }

            ViewBag.Query = query;

            if(String.IsNullOrWhiteSpace(index) || !indices.Contains(index))
            {
                return View("~/Views/ElasticSearchAdmin/Console/Index.cshtml");
            }

            string uri = $"{_settings.Host}/{index}/_search";
            if(_serverInfoService.GetInfo().Version >= Constants.TotalHitsAsIntAddedVersion)
            {
                uri += "?rest_total_hits_as_int=true";
            }

            byte[] data = Encoding.UTF8.GetBytes(query);
            byte[] returnData = _httpClientHelper.Post(new Uri(uri), data);
            string response = Encoding.UTF8.GetString(returnData);

            ViewBag.Result = JToken.Parse(response).ToString(Formatting.Indented);

            return View("~/Views/ElasticSearchAdmin/Console/Index.cshtml");
        }

        public ActionResult Mapping(string index = null) => GetJsonFromEndpoint(index, "mapping");

        public ActionResult Settings(string index = null) => GetJsonFromEndpoint(index, "settings");

        private ActionResult GetJsonFromEndpoint(string index, string endpoint)
        {
            if(String.IsNullOrWhiteSpace(index))
            {
                index = CurrentIndex;
            }

            var indexHelper = new Admin.Index(_serverInfoService, _settings, _httpClientHelper, index);

            var indices = indexHelper.GetIndices()
                .Select(i => i.Index).ToList();

            ViewBag.Indices = indices;
            ViewBag.Endpoint = endpoint;

            if(!indices.Contains(index))
            {
                return View("~/Views/ElasticSearchAdmin/Console/_JsonDump.cshtml");
            }

            ViewBag.SelectedIndex = index;

            string uri = $"{_settings.Host}/{index}/_{endpoint}";
            string response = _httpClientHelper.GetJson(new Uri(uri));

            ViewBag.Result = JToken.Parse(response).ToString(Formatting.Indented);

            return View("~/Views/ElasticSearchAdmin/Console/_JsonDump.cshtml");
        }
    }
}