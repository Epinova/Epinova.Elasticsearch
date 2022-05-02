using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer.DataAbstraction;
using EPiServer.Globalization;
using EPiServer.Personalization;
using EPiServer.Shell.Navigation;

namespace Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions
{
    [Authorize(Roles = RoleNames.ElasticsearchAdmins)]
    public abstract class ElasticSearchControllerBase : Controller
    {
        internal const string IndexParam = "index";
        internal const string LanguageParam = "languageId";

        private readonly ILanguageBranchRepository _languageBranchRepository;

        protected string CurrentIndex;
        protected string CurrentIndexDisplayName;
        protected string CurrentLanguage;
        private readonly IServerInfoService _serverInfoService;
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;

        protected ElasticSearchControllerBase(
            IServerInfoService serverInfoService,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper,
            ILanguageBranchRepository languageBranchRepository)
        {
            _serverInfoService = serverInfoService;
            _settings = settings;
            _httpClientHelper = httpClientHelper;
            _languageBranchRepository = languageBranchRepository;

            Indices = GetIndices();
            Languages = GetLanguages();
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            CurrentLanguage = Request?.QueryString[LanguageParam] ?? Languages.First().Key;
            var language = new CultureInfo(CurrentLanguage);
            
            CurrentIndex = Request?.QueryString[IndexParam] ?? _settings.GetDefaultIndexName(language);

            CurrentIndexDisplayName = Indices.FirstOrDefault(i => i.Index.Equals(_settings.GetCustomIndexName(CurrentIndex, language), StringComparison.OrdinalIgnoreCase))?.DisplayName;
        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            base.OnResultExecuting(filterContext);
            ViewBag.SelectedIndex = CurrentIndex;
            ViewBag.SelectedIndexName = CurrentIndexDisplayName;

            var platformNavigationMethod = typeof(MenuHelper).GetMethod("CreatePlatformNavigationMenu", Array.Empty<Type>());
            if(platformNavigationMethod != null)
            {
                ViewBag.Menu = platformNavigationMethod.Invoke(null, null)?.ToString();
                ViewBag.ContainerClass = "epi-navigation--fullscreen-fixed-adjust";
            }
            else
            {
                ViewBag.Menu = MenuHelper.CreateGlobalMenu(String.Empty, String.Empty);
            }
        }

        protected string SwapLanguage(string indexName, CultureInfo newLanguage)
        {
            if(String.IsNullOrEmpty(indexName))
            {
                return null;
            }

            return _settings.GetCustomIndexName(_settings.GetIndexNameWithoutLanguage(indexName), newLanguage);
        }

        protected List<IndexInformation> Indices { get; }

        protected Dictionary<string, string> UniqueIndices =>
            Indices.Select(i =>
                {
                    string lang = _settings.GetLanguageFromIndexName(i.Index);
                    //var nameWithoutLanguage = _settings.GetIndexNameWithoutLanguage(i.Index);

                        return string.IsNullOrWhiteSpace(CurrentLanguage) || CurrentLanguage.Equals(lang, StringComparison.OrdinalIgnoreCase)
                            ? new { i.Index, i.DisplayName }
                            : null;
                    })
                .Where(i => i != null)
                .Distinct()
                .ToDictionary(x => x.Index, x => x.DisplayName);

        protected Dictionary<string, string> UniqueIndicesNoLanguage =>
                UniqueIndices.Select(i => new { Index = _settings.GetIndexNameWithoutLanguage(i.Key), DisplayName = i.Value })
                .Distinct()
                .ToDictionary(x => x.Index, x => x.DisplayName);

        protected Dictionary<string, string> Languages { get; }

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);

            SystemLanguage.Instance.SetCulture();
            UserInterfaceLanguage.Instance.SetCulture(EPiServerProfile.Current?.Language);
        }

        private List<IndexInformation> GetIndices()
        {
            var indexHelper = new Admin.Index(_serverInfoService, _settings, _httpClientHelper, null);

            var indices = indexHelper.GetIndices().ToList();

            var config = ElasticSearchSection.GetConfiguration();

            foreach(var indexInfo in indices)
            {
                var parsed = config.IndicesParsed
                    .OrderByDescending(i => i.Name.Length)
                    .FirstOrDefault(i => indexInfo.Index.StartsWith(i.Name, StringComparison.InvariantCultureIgnoreCase));

                if(String.IsNullOrWhiteSpace(parsed?.Type))
                {
                    indexInfo.TypeName = "[default]";
                    indexInfo.Type = typeof(IndexItem);
                }
                else
                {
                    Type type = Type.GetType(parsed.Type);
                    indexInfo.TypeName = type?.FullName;
                    indexInfo.Type = type;
                }

                if(parsed?.Default ?? false)
                {
                    indexInfo.SortOrder = -1;
                }

                var displayName = new StringBuilder(parsed?.DisplayName);

                if(indexInfo.Index.Contains($"-{Constants.CommerceProviderName}".ToLowerInvariant()))
                {
                    displayName.Append(" Commerce");
                }

                indexInfo.DisplayName = displayName.ToString();
            }

            return indices
                .OrderBy(i => i.SortOrder)
                .ToList();
        }

        private Dictionary<string, string> GetLanguages()
        {
            return _languageBranchRepository.ListEnabled()
                .ToDictionary(x => x.LanguageID, x => x.Name);
        }
    }
}