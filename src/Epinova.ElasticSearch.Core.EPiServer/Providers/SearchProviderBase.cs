using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Settings;
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
    public abstract class SearchProviderBase<TSearchType, TContentData, TContentType> :
        ContentSearchProviderBase<TContentData, TContentType>
        where TContentType : ContentType
        where TContentData : IContentData
        where TSearchType : class
    {
        private readonly string _categoryKey;
        protected string IndexName;

        // ReSharper disable StaticMemberInGenericType
#pragma warning disable 649
        private static Injected<LocalizationService> _localizationServiceHelper;
#pragma warning restore 649
        // ReSharper restore StaticMemberInGenericType
        protected readonly IElasticSearchService<TSearchType> _elasticSearchService;
        protected readonly IElasticSearchSettings _elasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();


        protected SearchProviderBase(string categoryKey)
            : base(
                _localizationServiceHelper.Service,
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
            _elasticSearchService = new ElasticSearchService<TSearchType>();
        }


        public override string Area => AreaName;

        protected string AreaName { private get; set; }

        public override string Category => _localizationServiceHelper.Service.GetString("/epinovaelasticsearch/providers/" + _categoryKey);

        protected string IconClass { private get; set; }

        protected override string IconCssClass => IconClass;

        protected bool ForceRootLookup { get; set; }

        public override IEnumerable<SearchResult> Search(Query query)
        {
            List<ContentSearchHit<TContentData>> contentSearchHits = new List<ContentSearchHit<TContentData>>();

            CultureInfo language = GetLanguage();

            if (!query.SearchRoots.Any() || ForceRootLookup)
                query.SearchRoots = new[] { GetSearchRoot() };

            foreach (string searchRoot in query.SearchRoots)
            {
                if(!Int32.TryParse(searchRoot, out int searchRootId))
                {
                    if (searchRoot.Contains("__"))
                        Int32.TryParse(searchRoot.Split(new[] { "__" }, StringSplitOptions.None)[0], out searchRootId);
                }

                if (searchRootId != 0)
                {
                    var searchQuery = CreateQuery(query, language, searchRootId);
                    ContentSearchResult<TContentData> contentSearchResult =
                        searchQuery.GetContentResults(providerNames: GetProviderKeys());

                    contentSearchHits.AddRange(contentSearchResult.Hits);
                }
            }

            return
                contentSearchHits.OrderByDescending(hit => hit.Score)
                    .Take(_elasticSearchSettings.ProviderMaxResults)
                    .Select(hit => CreateSearchResult(hit.Content));
        }

        protected virtual string GetSearchRoot()
        {
            return ContentReference.RootPage.ID.ToString();
        }

        protected virtual string[] GetProviderKeys()
        {
            return null;
        }


        private IElasticSearchService<TContentData> CreateQuery(Query query, CultureInfo language, int searchRootId)
        {
            return _elasticSearchService
                .WildcardSearch<TContentData>(String.Concat("*", query.SearchQuery, "*"))
                .UseIndex(IndexName)
                .Language(language)
                .Boost(x => DefaultFields.Name, 20)
                .Boost(x => DefaultFields.Id, 10)
                .StartFrom(searchRootId)
                .Take(_elasticSearchSettings.ProviderMaxResults);
        }

        protected static CultureInfo GetLanguage()
        {
            HttpContext context = HttpContext.Current;

            if (context != null && context.Request.Headers.AllKeys.Contains("X-EPiContentLanguage"))
                return CultureInfo.CreateSpecificCulture(context.Request.Headers["X-EPiContentLanguage"]);

            return CultureInfo.InvariantCulture;
        }
    }
}