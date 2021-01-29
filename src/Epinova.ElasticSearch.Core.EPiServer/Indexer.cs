using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Hosting;
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using EPiServer.Web;

namespace Epinova.ElasticSearch.Core.EPiServer
{
    [ServiceConfiguration(typeof(IIndexer))]
    internal class Indexer : IIndexer
    {
        private const string FallbackLanguage = "en";
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Indexer));
        private readonly ICoreIndexer _coreIndexer;
        private readonly ContentAssetHelper _contentAssetHelper;
        private readonly IContentLoader _contentLoader;
        private readonly IElasticSearchSettings _elasticSearchSettings;
        private readonly ISiteDefinitionRepository _siteDefinitionRepository;

        // Avoid dependency on Episerver.Forms for this simple functionallity
        internal const string FormsUploadNamespace = "EPiServer.Forms.Core.IFileUploadElementBlock";

        internal void SetContentPathGetter(Func<ContentReference, int[]> func)
            => _getContentPath = func;

        private Func<ContentReference, int[]> _getContentPath = ContentExtensions.GetContentPath;

        public Indexer(
            ICoreIndexer coreIndexer,
            IElasticSearchSettings elasticSearchSettings,
            ISiteDefinitionRepository siteDefinitionRepository,
            IContentLoader contentLoader,
            ContentAssetHelper contentAssetHelper)
        {
            _coreIndexer = coreIndexer;
            _elasticSearchSettings = elasticSearchSettings;
            _siteDefinitionRepository = siteDefinitionRepository;
            _contentLoader = contentLoader;
            _contentAssetHelper = contentAssetHelper;
        }

        public BulkBatchResult BulkUpdate(IEnumerable<IContent> contents, Action<string> logger, string indexName = null)
        {
            var contentList = contents.ToList();
            logger = logger ?? delegate { };
            var before = contentList.Count;

            logger("Filtering away content of excluded types and content with property HideFromSearch enabled...");

            contentList.RemoveAll(SkipIndexing);
            contentList.RemoveAll(IsExcludedType);

            logger($"Filtered away content of excluded types and content with property HideFromSearch enabled... {before - contentList.Count} of {before} items removed. Next will IContent be converted to indexable items and added to indexed. Depending on number of IContent items this is a time-consuming task.");

            var operations =
                contentList.Select(
                        content =>
                        {
                            var language = GetLanguage(content);
                            var index = String.IsNullOrWhiteSpace(indexName)
                                    ? _elasticSearchSettings.GetDefaultIndexName(language)
                                    : _elasticSearchSettings.GetCustomIndexName(indexName, language);

                            return new BulkOperation(
                                content.AsIndexItem(),
                                Operation.Index,
                                GetLanguage(content),
                                typeof(IndexItem),
                                content.ContentLink.ToReferenceWithoutVersion().ToString(),
                                index);
                        }
                    )
                    .Where(b => b.Data != null)
                    .ToList();

            logger($"Initializing bulk operation... Bulk indexing {operations.Count} items");

            return _coreIndexer.Bulk(operations, logger);
        }

        public void Delete(ContentReference contentLink)
        {
            var language = Utilities.Language.GetRequestLanguageCode();

            var indexName = GetIndexname(contentLink, null, language);
            _coreIndexer.Delete(contentLink.ToReferenceWithoutVersion().ToString(), language, typeof(IndexItem), indexName);
        }

        public void Delete(IContent content, string indexName = null)
        {
            indexName = GetIndexname(content.ContentLink, indexName, GetLanguage(content));

            _coreIndexer.Delete(content.ContentLink.ToReferenceWithoutVersion().ToString(), GetLanguage(content), typeof(IndexItem), indexName);
        }

        public IndexingStatus UpdateStructure(IContent root, string indexName = null)
        {
            var language = CultureInfo.CurrentCulture;
            if(root is ILocale locale && locale.Language != null && !CultureInfo.InvariantCulture.Equals(locale.Language))
            {
                language = locale.Language;
            }

            Logger.Information($"Performing recursive update, starting from {root.ContentLink}, in language {language}");

            var status = Update(root, indexName);
            var descendents = _contentLoader.GetDescendents(root.ContentLink);

            foreach(var content in _contentLoader.GetItems(descendents, language))
            {
                var childStatus = Update(content, indexName);

                if(childStatus == IndexingStatus.Error && status != IndexingStatus.Error)
                {
                    status = IndexingStatus.PartialError;
                }
            }

            return status;
        }

        public IndexingStatus Update(IContent content, string indexName = null)
        {
            indexName = GetIndexname(content.ContentLink, indexName, GetLanguage(content));

            if(SkipIndexing(content))
            {
                Delete(content, indexName);
                return IndexingStatus.HideFromSearchProperty;
            }

            if(IsExcludedType(content))
            {
                return IndexingStatus.ExcludedByConvention;
            }

            if(IsExcludedByRoot(content))
            {
                return IndexingStatus.ExcludedByConvention;
            }

            _coreIndexer.Update(content.ContentLink.ToReferenceWithoutVersion().ToString(), content.AsIndexItem(), indexName, typeof(IndexItem));

            return IndexingStatus.Ok;
        }

        private bool IsExcludedByRoot(IContent content)
        {
            // Check options less expensive than DB-lookup first
            if(content.ContentLink != null && Indexing.ExcludedRoots.Contains(content.ContentLink.ID))
            {
                return true;
            }

            if(content.ParentLink != null && Indexing.ExcludedRoots.Contains(content.ParentLink.ID))
            {
                return true;
            }

            var ancestors = _getContentPath(content.ContentLink);

            if(ancestors == null)
            {
                return false;
            }

            return Indexing.ExcludedRoots.Intersect(ancestors).Any();
        }

        public bool IsExcludedType(IContent content)
        {
            return (content?.GetUnproxiedType()).IsExcludedType() || (content?.GetType()).IsExcludedType();
        }
        

        public bool SkipIndexing(IContent content)
        {
            if(content is ContentFolder)
            {
                return true;
            }

            if(ContentReference.WasteBasket.CompareToIgnoreWorkID(content.ParentLink))
            {
                return true;
            }

            if(ContentReference.WasteBasket.CompareToIgnoreWorkID(content.ContentLink))
            {
                return true;
            }

            if(IsFormUpload(content))
            {
                return true;
            }

            var deleted = GetEpiserverBoolProperty(content.Property["PageDeleted"]);
            if(deleted)
            {
                return true;
            }

            if(IsPageWithInvalidLinkType(content))
            {
                return true;
            }

            return false;
        }

        public bool ShouldHideFromSearch(IContent content)
        {
            //This is already called to avoid indexing
            if(SkipIndexing(content))
            {
                return true;
            }

            // Common property in Epinova template
            var hideFromSearch = GetEpiserverBoolProperty(content.Property["HideFromSearch"]);

            return hideFromSearch;
        }

        private static bool IsPageWithInvalidLinkType(IContent content)
        {
            if(!(content is PageData))
            {
                return false;
            }

            var linkType = content.GetType().GetProperty("LinkType");
            if(linkType == null)
            {
                return true;
            }

            var linkTypeValue = linkType.GetValue(content);
            if(linkTypeValue == null)
            {
                return true;
            }

            var shortcutType = (PageShortcutType)linkTypeValue;
            if(shortcutType != PageShortcutType.Normal && shortcutType != PageShortcutType.FetchData)
            {
                return true;
            }

            return false;
        }

        private string GetFallbackLanguage()
        {
            if(!HostingEnvironment.IsHosted)
            {
                return FallbackLanguage;
            }

            ContentReference startPageLink = ContentReference.StartPage;
            if(startPageLink == null || startPageLink == ContentReference.EmptyReference)
            {
                // Fallback to first defined site if StartPage is empty (no context or star-mapping)
                var firstSite = _siteDefinitionRepository.List().FirstOrDefault();
                if(firstSite != null)
                {
                    startPageLink = firstSite.StartPage;
                }
            }

            // Try to fetch master language from startpage
            if(startPageLink != null && startPageLink != ContentReference.EmptyReference)
            {
                var contentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
                if(contentLoader.TryGet(startPageLink, out PageData startPage))
                {
                    return startPage.MasterLanguage.Name;
                }
            }

            Logger.Warning("Could not retrieve StartPage. Are you missing a wildcard mapping in CMS Admin -> Manage Websites?");

            return FallbackLanguage;
        }

        public string GetLanguage(IContent content)
        {
            return content is ILocale localizable
                   && localizable.Language?.Equals(CultureInfo.InvariantCulture) == false
                ? localizable.Language.Name
                : GetFallbackLanguage();
        }

        private static bool GetEpiserverBoolProperty(PropertyData content)
            => content is PropertyBoolean property && property.Boolean.GetValueOrDefault(false);

        private bool IsFormUpload(IContent content)
        {
            IContent owner = _contentAssetHelper.GetAssetOwner(content.ContentLink);

            if(owner == null)
            {
                return false;
            }

            return owner.GetType().GetInterfaces()
                .Select(i => i.FullName)
                .Contains(FormsUploadNamespace);
        }

        private string GetIndexname(ContentReference contentLink, string indexName, string language)
        {
            if(String.IsNullOrWhiteSpace(indexName) && contentLink.ProviderName == null)
            {
                return _elasticSearchSettings.GetDefaultIndexName(language);
            }
            else
            {
                return _elasticSearchSettings.GetCustomIndexName($"{_elasticSearchSettings.Index}-{Constants.CommerceProviderName}", language);
            }
        }
    }
}
