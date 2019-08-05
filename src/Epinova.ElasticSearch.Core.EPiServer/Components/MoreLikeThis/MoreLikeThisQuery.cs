using System.Collections.Generic;
using System.Linq;
using System.Web;
using Epinova.ElasticSearch.Core.EPiServer.Providers;
using EPiServer;
using EPiServer.Cms.Shell.UI.Rest.ContentQuery;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ContentQuery;
using EPiServer.Shell.Search;

namespace Epinova.ElasticSearch.Core.EPiServer.Components.MoreLikeThis
{
    [ServiceConfiguration(typeof(IContentQuery))]
    public class MoreLikeThisQuery : ContentQueryBase
    {
        private readonly IContentRepository _contentRepository;
        private readonly SearchProvidersManager _searchProvidersManager;
        private readonly LanguageSelectorFactory _languageSelectorFactory;

        public MoreLikeThisQuery(
            IContentQueryHelper queryHelper,
            IContentRepository contentRepository,
            SearchProvidersManager searchProvidersManager,
            LanguageSelectorFactory languageSelectorFactory)
            : base(contentRepository, queryHelper)
        {
            _contentRepository = contentRepository;
            _searchProvidersManager = searchProvidersManager;
            _languageSelectorFactory = languageSelectorFactory;
        }

        /// <summary>
        /// The key to trigger this query.
        /// </summary>
        public override string Name => nameof(MoreLikeThisQuery);

        protected override IEnumerable<IContent> GetContent(ContentQueryParameters parameters)
        {
            var queryText = HttpUtility.HtmlDecode(parameters.AllParameters["queryText"]);
            var area = ProviderConstants.PageArea;
            var contentLink = ContentReference.Parse(queryText);
            if(contentLink.ProviderName == ProviderConstants.CatalogProviderKey)
            {
                area = ProviderConstants.CommerceCatalogArea;
            }

            var searchQuery = new Query(queryText);
            searchQuery.Parameters.Add(Core.Models.Constants.MoreLikeThisId, queryText);

            var contentReferences = Enumerable.Empty<ContentReference>();
            var searchProvider = _searchProvidersManager.GetEnabledProvidersByPriority(area, true).FirstOrDefault();

            if(searchProvider != null)
            {
                contentReferences = searchProvider.Search(searchQuery)
                    .Select(r => ContentReference.Parse(r.Metadata["Id"]))
                    .Distinct();
            }

            return _contentRepository.GetItems(contentReferences,
                _languageSelectorFactory.AutoDetect(parameters.AllLanguages));
        }
    }
}
