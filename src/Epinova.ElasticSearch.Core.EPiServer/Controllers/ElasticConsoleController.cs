using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.DataAbstraction;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers
{
    public class ElasticConsoleController : ElasticSearchControllerBase
    {
        private readonly IElasticSearchSettings _settings;

        public ElasticConsoleController(
            ILanguageBranchRepository languageBranchRepository,
            IElasticSearchSettings settings) : base(settings, languageBranchRepository)
        {
            _settings = settings;
        }

        [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
        [ValidateInput(false)]
        public ActionResult Index(string query, string index)
        {
            var indexHelper = new Admin.Index(_settings);

            List<string> indices = indexHelper.GetIndices()
                .Select(i => i.Index).ToList();

            if (String.IsNullOrWhiteSpace(index) || !indices.Contains(index))
                index = _settings.Index + "-*";

            ViewBag.Indices = indices;
            ViewBag.SelectedIndex = index;

            if (String.IsNullOrWhiteSpace(query))
            {
                query = "{\n    \"size\" : 10,\n    \"query\" : {\n        \"match_all\" : {}\n    }\n}";
            }

            ViewBag.Query = query;

            if (String.IsNullOrWhiteSpace(index) || !indices.Contains(index))
                return View("~/Views/ElasticSearchAdmin/Console/Index.cshtml");

            string uri = $"{_settings.Host}/{index}/_search";
            byte[] data = Encoding.UTF8.GetBytes(query);
            byte[] returnData = HttpClientHelper.Post(new Uri(uri), data);
            string response = Encoding.UTF8.GetString(returnData);

            ViewBag.Result = JToken.Parse(response).ToString(Formatting.Indented);

            return View("~/Views/ElasticSearchAdmin/Console/Index.cshtml");
        }

        [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
        public ActionResult Mapping(string index = null)
        {
            return GetJsonFromEndpoint(index, "mapping");
        }

        [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
        public ActionResult Settings(string index = null)
        {
            return GetJsonFromEndpoint(index, "settings");
        }

        private ActionResult GetJsonFromEndpoint(string index, string endpoint)
        {
            var indexHelper = new Admin.Index(_settings);

            var indices = indexHelper.GetIndices()
                .Select(i => i.Index).ToList();

            ViewBag.Indices = indices;
            ViewBag.Endpoint = endpoint;

            if (String.IsNullOrWhiteSpace(index) || !indices.Contains(index))
                return View("~/Views/ElasticSearchAdmin/Console/_JsonDump.cshtml");

            ViewBag.SelectedIndex = index;

            string uri = $"{_settings.Host}/{index}/_{endpoint}";
            string response = HttpClientHelper.GetJson(new Uri(uri));

            ViewBag.Result = JToken.Parse(response).ToString(Formatting.Indented);

            return View("~/Views/ElasticSearchAdmin/Console/_JsonDump.cshtml");
        }
    }
}
