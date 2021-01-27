using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Cms.Shell.Search;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Framework.Localization;
using EPiServer.Globalization;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Search;
using EPiServer.Web;
using EPiServer.Web.Routing;
using EditUrlResolver = EPiServer.Web.Routing.EditUrlResolver;

namespace Epinova.ElasticSearch.Core.EPiServer.Providers
{
    public abstract class SearchProviderBase<TSearchType, TContentData, TContentType> : ContentSearchProviderBase<TContentData, TContentType>
        where TSearchType : class
        where TContentData : IContentData
        where TContentType : ContentType
    {
        private readonly string _categoryKey;
        protected string IndexName;
     
        protected readonly IElasticSearchService<TSearchType> _elasticSearchService;
        protected readonly IElasticSearchSettings _elasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        protected readonly IServerInfoService _serverInfoService = ServiceLocator.Current.GetInstance<IServerInfoService>();
        protected readonly IHttpClientHelper _httpClientHelper = ServiceLocator.Current.GetInstance<IHttpClientHelper>();
        protected readonly LocalizationService _localizationService = ServiceLocator.Current.GetInstance<LocalizationService>();

        protected SearchProviderBase(string categoryKey)
            : base(
                ServiceLocator.Current.GetInstance<LocalizationService>(),
                ServiceLocator.Current.GetInstance<ISiteDefinitionResolver>(),
                ServiceLocator.Current.GetInstance<IContentTypeRepository<TContentType>>(),
                ServiceLocator.Current.GetInstance<EditUrlResolver>(),
                ServiceLocator.Current.GetInstance<ServiceAccessor<SiteDefinition>>(),
                ServiceLocator.Current.GetInstance<LanguageResolver>(),
                ServiceLocator.Current.GetInstance<UrlResolver>(),
                ServiceLocator.Current.GetInstance<TemplateResolver>(),
                ServiceLocator.Current.GetInstance<UIDescriptorRegistry>())
        {
            _categoryKey = categoryKey;
            _elasticSearchService = new ElasticSearchService<TSearchType>(_serverInfoService, _elasticSearchSettings, _httpClientHelper);
        }

        public override string Area => AreaName;

        protected string AreaName { private get; set; }

        public override string Category => _localizationService.GetString("/epinovaelasticsearch/providers/" + _categoryKey);

        protected string IconClass { private get; set; }

        protected override string IconCssClass => IconClass;

        protected bool ForceRootLookup { get; set; }

        public override IEnumerable<SearchResult> Search(Query query)
        {
            var contentSearchHits = new List<ContentSearchHit<TContentData>>();
            CultureInfo language = Language.GetRequestLanguage();

            if(!query.SearchRoots.Any() || ForceRootLookup)
            {
                query.SearchRoots = new[] { GetSearchRoot() };
            }

            foreach(string searchRoot in query.SearchRoots)
            {
                if(!Int32.TryParse(searchRoot, out int searchRootId) && searchRoot.Contains("__"))
                {
                    Int32.TryParse(searchRoot.Split(new[] { "__" }, StringSplitOptions.None)[0], out searchRootId);
                }

                if(searchRootId != 0)
                {
                    IElasticSearchService<TContentData> searchQuery = CreateQuery(query, language, searchRootId);

                    ContentSearchResult<TContentData> contentSearchResult =
                        searchQuery.GetContentResults(false, true, GetProviderKeys(), false, false);

                    contentSearchHits.AddRange(contentSearchResult.Hits);
                }
            }

            return
                contentSearchHits.OrderByDescending(hit => hit.Score)
                    .Take(_elasticSearchSettings.ProviderMaxResults)
                    .Select(hit => CreateSearchResult(hit.Content));
        }

        protected virtual string GetSearchRoot() => ContentReference.RootPage.ID.ToString();

        protected virtual string[] GetProviderKeys() => Array.Empty<string>();

        private IElasticSearchService<TContentData> CreateQuery(Query query, CultureInfo language, int searchRootId)
        {
            if(query.Parameters.TryGetValue(Core.Models.Constants.MoreLikeThisId, out object mltId) && mltId != null)
            {
                var id = ContentReference.Parse(mltId.ToString()).ToReferenceWithoutVersion();

                var esQuery = _elasticSearchService
                    .MoreLikeThis<TContentData>(id.ToString(), minimumWordLength: 2, minimumDocFrequency: 1)
                    .UseIndex(IndexName)
                    .Language(language)
                    .StartFrom(searchRootId)
                    .Take(_elasticSearchSettings.ProviderMaxResults);

                foreach(var field in Conventions.MoreLikeThis.ComponentFields.Keys.ToArray())
                {
                    esQuery = esQuery.InField(_ => field);
                }

                return esQuery;
            }

            return _elasticSearchService
                .WildcardSearch<TContentData>(String.Concat("*", query.SearchQuery, "*"))
                .UseIndex(IndexName)
                .Language(language)
                .Boost(_ => DefaultFields.Name, 20)
                .Boost(_ => DefaultFields.Id, 10)
                .StartFrom(searchRootId)
                .Take(_elasticSearchSettings.ProviderMaxResults);
        }
    }
}