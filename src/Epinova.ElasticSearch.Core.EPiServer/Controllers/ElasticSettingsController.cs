using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ElasticSettingsController : ElasticSearchControllerBase
    {
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;

        public ElasticSettingsController(ILanguageBranchRepository languageBranchRepository, IElasticSearchSettings settings, IServerInfoService serverInfoService, IHttpClientHelper httpClientHelper) : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
        {
            _settings = settings;
            _httpClientHelper = httpClientHelper;
        }

        public ActionResult Index(string query, string index)
        {
            if(string.IsNullOrWhiteSpace(index))
            {
                index = _settings.GetDefaultIndexName(ContentLanguage.PreferredCulture);
            }

            List<string> indices = Indices.Select(i => i.Index).ToList();

            SettingsViewModel model = new SettingsViewModel(index, indices);

            string uri = $"{_settings.Host}/{index}/_settings";
            string response = _httpClientHelper.GetJson(new Uri(uri));

            model.Result = JToken.Parse(response).ToString(Formatting.Indented);

            return View("~/Views/ElasticSearchAdmin/Settings/Index.cshtml", model);
        }
    }
}