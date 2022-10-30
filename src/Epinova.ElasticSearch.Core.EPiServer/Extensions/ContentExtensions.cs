using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Castle.DynamicProxy;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Providers;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.DataAccess.Internal;
using EPiServer.Filters;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Indexing = Epinova.ElasticSearch.Core.Conventions.Indexing;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    //TODO: Refactor, this is not an extension class anymore
    public static class ContentExtensions
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ContentExtensions));
        private static readonly IContentLoader ContentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
        private static readonly IElasticSearchSettings ElasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        private static readonly ITrackingRepository TrackingRepository = ServiceLocator.Current.GetInstance<ITrackingRepository>();
        private static readonly IIndexer Indexer = ServiceLocator.Current.GetInstance<IIndexer>();
        private static readonly IBestBetsRepository BestBetsRepository = ServiceLocator.Current.GetInstance<IBestBetsRepository>();

        private static readonly string[] BinaryExtensions = new[]
        {
            "jpg", "jpeg", "gif", "psd", "bmp", "ai", "webp", "tif", "tiff", "ico", "jif", "png", "xcf", "eps", "raw", "cr2", "pct", "bpg",
            "exe", "zip", "rar", "7z", "dll", "gz", "bin", "iso", "apk", "dmp", "msi",
            "mp4", "mkv", "avi", "mov", "mpg", "mpeg", "vob", "flv", "h264", "m4v", "swf", "wmv",
            "mp3", "aac", "wav", "flac", "ogg", "mka", "wma", "aif", "mpa"
        };

        public static Task<ContentSearchResult<T>> GetContentResultsAsync<T>(this IElasticSearchService<T> service) where T : IContentData
            => service.GetContentResultsAsync(false, new string[0]);

        public static Task<ContentSearchResult<T>> GetContentResultsAsync<T>(this IElasticSearchService<T> service, bool requirePageTemplate) where T : IContentData
            => service.GetContentResultsAsync(requirePageTemplate, new string[0]);

        public static Task<ContentSearchResult<T>> GetContentResultsAsync<T>(this IElasticSearchService<T> service, bool requirePageTemplate, string[] providerNames) where T : IContentData
            => service.GetContentResultsAsync(CancellationToken.None, requirePageTemplate, false, providerNames, true, true);

        public static async Task<ContentSearchResult<T>> GetContentResultsAsync<T>(
            this IElasticSearchService<T> service,
            CancellationToken cancellationToken,
            bool requirePageTemplate,
            bool ignoreFilters,
            string[] providerNames,
            bool enableHighlighting,
            bool enableDidYouMean) where T : IContentData
        {
            SearchResult results = await service.GetResultsAsync(cancellationToken, enableHighlighting, enableDidYouMean, !ignoreFilters, DefaultFields.Id).ConfigureAwait(false);

            IEnumerable<Task<ContentSearchHit<T>>> tasks =
                results.Hits.Select(h => FilterAsync<T>(h, requirePageTemplate, providerNames, ignoreFilters));

            ContentSearchHit<T>[] hits = await Task.WhenAll(tasks).ConfigureAwait(false);
            hits = hits.Where(h => h != null).ToArray();

            results.TotalHits -= hits.Count(h => h == null);

            if(service.TrackSearch)
                TrackingRepository.AddSearch(service, results.TotalHits == 0);

            return new ContentSearchResult<T>(results, hits);
        }

        public static ContentSearchResult<T> GetContentResults<T>(this IElasticSearchService<T> service) where T : IContentData
            => service.GetContentResults(false, new string[0]);

        public static ContentSearchResult<T> GetContentResults<T>(this IElasticSearchService<T> service, bool requirePageTemplate) where T : IContentData
            => service.GetContentResults(requirePageTemplate, new string[0]);

        public static ContentSearchResult<T> GetContentResults<T>(this IElasticSearchService<T> service, bool requirePageTemplate, string[] providerNames) where T : IContentData
            => service.GetContentResults(requirePageTemplate, ignoreFilters: false, providerNames, enableHighlighting: true, enableDidYouMean: true);

        public static ContentSearchResult<T> GetContentResults<T>(
            this IElasticSearchService<T> service,
            bool requirePageTemplate,
            bool ignoreFilters,
            string[] providerNames,
            bool enableHighlighting,
            bool enableDidYouMean) where T : IContentData
        {
            SearchResult results = service.GetResults(enableHighlighting, enableDidYouMean, applyDefaultFilters: !ignoreFilters);
            var hits = new List<ContentSearchHit<T>>();
            
            foreach(SearchHit hit in results.Hits)
            {
                if(ShouldAdd(hit, requirePageTemplate, out T content, providerNames, ignoreFilters))
                {
                    hits.Add(new ContentSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight));
                }
                else
                {
                    results.TotalHits--;
                }
            }

            if(service.TrackSearch)
                TrackingRepository.AddSearch(service, results.TotalHits == 0);

            return new ContentSearchResult<T>(results, hits);
        }

        private static async Task<ContentSearchHit<T>> FilterAsync<T>(SearchHit hit, bool requirePageTemplate, string[] providerNames, bool ignoreFilters = true) where T : IContentData
        {
            return await Task.Run(() =>
            {
                if(!ShouldAdd(hit, requirePageTemplate, out T content, providerNames, ignoreFilters))
                {
                    return null;
                }

                return new ContentSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight);
            }).ConfigureAwait(false);
        }

        internal static bool ShouldAdd<T>(this SearchHit hit, bool requirePageTemplate, out T content, string[] providerNames, bool ignoreFilters)
            where T : IContentData
        {
            if(providerNames.Length > 0)
            {
                content = GetContentForProviders<T>(hit, providerNames);
            }
            else
            {
                var contentLink = new ContentReference(hit.Id);

                if(!String.IsNullOrEmpty(hit.Lang))
                {
                    ContentLoader.TryGet(contentLink, CultureInfo.GetCultureInfo(hit.Lang), out content);
                }
                else
                {
                    ContentLoader.TryGet(contentLink, out content);
                }
            }

            return content != null
                && !ShouldFilter(content as IContent, requirePageTemplate, ignoreFilters);
        }

        private static T GetContentForProviders<T>(SearchHit hit, string[] providerNames) where T : IContentData
        {
            foreach(var name in providerNames)
            {
                ContentReference contentLink =
                    new[] { ProviderConstants.CmsProviderKey, ProviderConstants.DefaultProviderKey }.Contains(name)
                        ? new ContentReference(hit.Id)
                        : new ContentReference(hit.Id, 0, name);

                if(!String.IsNullOrEmpty(hit.Lang))
                {
                    if(ContentLoader.TryGet(contentLink, CultureInfo.GetCultureInfo(hit.Lang), out T content))
                    {
                        return content;
                    }
                }
                else
                {
                    if(ContentLoader.TryGet(contentLink, out T content))
                    {
                        return content;
                    }
                }
            }

            return default;
        }

        private static bool ShouldFilter(IContent content, bool requirePageTemplate, bool ignoreFilters)
        {
            if(content == null)
            {
                return true;
            }

            if(ignoreFilters)
            {
                return false;
            }

            if(Indexer.ShouldHideFromSearch(content))
            {
                return true;
            }

            var accessFilter = new FilterAccess();
            var publishedFilter = new FilterPublished();

            if(publishedFilter.ShouldFilter(content) || accessFilter.ShouldFilter(content))
            {
                return true;
            }

            var templateFilter = new FilterTemplate();

            return requirePageTemplate && templateFilter.ShouldFilter(content);
        }

        internal static int[] GetContentPath(ContentReference contentLink)
        {
            ContentPath contentPath = ServiceLocator.Current.GetInstance<ContentPathDB>().GetContentPath(contentLink);

            // Commerce doesn't support ContentPath
            if(contentLink.ProviderName == ProviderConstants.CatalogProviderKey)
            {
                return GetPathTheHardWay(contentLink);
            }

            if(contentPath?.Any() != true)
            {
                return Array.Empty<int>();
            }

            return contentPath
                .ToString()
                .Split('.')
                .Select(Int32.Parse)
                .ToArray();
        }

        private static int[] GetPathTheHardWay(ContentReference contentLink)
        {
            var path = new List<int>();
            ContentReference current = contentLink;
            while(current != null)
            {
                path.Add(current.ID);

                if(ContentLoader.TryGet(current, out IContent content))
                {
                    current = content.ParentLink;
                }
                else
                {
                    current = null;
                }
            }

            path.Reverse();

            return path.ToArray();
        }

        internal static dynamic AsIndexItem(this IContent content)
        {
            Logger.Debug("Indexing: " + content.Name + " / " + content.ContentLink);

            Type contentType = GetContentType(content);
            dynamic indexItem = new ExpandoObject();
            var dictionary = (IDictionary<string, object>)indexItem;

            if(!TryAddAttachmentData(content, dictionary))
            {
                return null;
            }

            AppendDefaultFields(content, dictionary, contentType);
            AppendIndexableProperties(indexItem, content, contentType, dictionary);
            AppendCustomProperties(content, contentType, dictionary);

            return indexItem;
        }

        private static void AppendCustomProperties(IContent content, Type contentType, IDictionary<string, object> dictionary)
        {
            List<CustomProperty> customProperties = GetCustomPropertiesForType(contentType);

            Logger.Debug("CustomProperties count: " + customProperties.Count);

            foreach(CustomProperty property in customProperties)
            {
                try
                {
                    object value;
                    Type returnType = property.Getter.Method.ReturnType;
                    bool isArrayCandidate = ArrayHelper.IsArrayCandidate(returnType);
                    bool isDictionary = ArrayHelper.IsDictionary(returnType);

                    // Set returnType to underlying type for nullable value-types
                    if(returnType.IsValueType && returnType.IsGenericType && returnType.GenericTypeArguments.Length > 0)
                    {
                        returnType = returnType.GenericTypeArguments[0];
                    }

                    if(returnType.IsValueType && !Mapping.IsNumericType(returnType))
                    {
                        value = returnType == typeof(bool)
                            ? SerializeValue(property.Getter.DynamicInvoke(content))
                            : SerializeValue(property.Getter.DynamicInvoke());
                    }
                    else if(isDictionary)
                    {
                        value = ArrayHelper.ToDictionary(property.Getter.DynamicInvoke(content));
                    }
                    else if(isArrayCandidate)
                    {
                        value = ArrayHelper.ToArray(property.Getter.DynamicInvoke(content));

                        if(value is IEnumerable<object> arrayValue && !arrayValue.Any())
                        {
                            Logger.Debug($"Value for '{property.Name}' is empty array value, skipping");
                            continue;
                        }
                    }
                    else
                    {
                        value = SerializeValue(property.Getter.DynamicInvoke(content));
                    }

                    if(returnType == typeof(CategoryList))
                    {
                        returnType = typeof(int[]);
                    }

                    if(value != null)
                    {
                        if(!isArrayCandidate && !isDictionary)
                        {
                            Logger.Debug($"Changing type of value '{value}' to '{returnType.Name}'");
                            value = Convert.ChangeType(value, returnType, CultureInfo.InvariantCulture);

                            if(returnType.IsAssignableFrom(typeof(string)))
                            {
                                value = value?.ToString().Trim('\"');
                            }
                        }

                        dictionary[property.Name] = value;
                    }

                    Logger.Debug("Name: " + property.Name);
                    Logger.Debug("Value: " + value);
                    Logger.Debug("Type: " + returnType.Name);
                }
                catch(Exception ex)
                {
                    Logger.Error("Failed to index custom property: " + property.Name, ex);
                }
            }
        }

        private static void TryAddLanguageProperty(dynamic indexItem, IContent content, IDictionary<string, object> dictionary, out CultureInfo language)
        {
            language = null;
            
            if(!(content is ILocale locale) || locale.Language == null || CultureInfo.InvariantCulture.Equals(locale.Language))
                return;
            
            language = locale.Language;
            dictionary.Add(DefaultFields.Lang, language);

            if(Logger.IsDebugEnabled())
            {
                string lang = indexItem.Lang.ToString();
                Logger.Debug("Language: " + lang);
            }

            bool isCommerceContent = content.ContentLink.ProviderName == ProviderConstants.CatalogProviderKey;

            string index = isCommerceContent
                ? ElasticSearchSettings.GetCommerceIndexName(language)
                : ElasticSearchSettings.GetDefaultIndexName(language);

            IEnumerable<string> bestBets = BestBetsRepository.GetBestBetsForContent(language, content.ContentLink.ID, index, isCommerceContent);
            dictionary.Add(DefaultFields.BestBets, bestBets);
        }

        private static bool TryAddAttachmentData(IContent content, IDictionary<string, object> dictionary)
        {
            string attachmentData = GetAttachmentData(content, out bool extensionNotAllowed);
            if(extensionNotAllowed)
            {
                return false;
            }

            if(attachmentData != null)
            {
                dictionary.Add(DefaultFields.AttachmentData, attachmentData);
            }

            return true;
        }

        private static void AppendDefaultFields(IContent content, IDictionary<string, object> dictionary, Type contentType)
        {
            string typeName = contentType.GetTypeName();

            dictionary.Add(DefaultFields.Id, content.ContentLink.ID);
            dictionary.Add(DefaultFields.Indexed, DateTime.Now);

            if(content.ContentLink != null)
            {
                dictionary.Add(DefaultFields.ContentLink, content.ContentLink);
            }

            if(content.ParentLink != null)
            {
                dictionary.Add(DefaultFields.ParentId, content.ParentLink.ID);
                dictionary.Add(DefaultFields.ParentLink, content.ParentLink);
            }

            dictionary.Add(DefaultFields.Name, content.Name);
            dictionary.Add(DefaultFields.Type, typeName);
            dictionary.Add(DefaultFields.Types, contentType.GetInheritancHierarchyArray());

            int[] contentPath = GetContentPath(content.ContentLink);
            if(contentPath.Length > 0)
            {
                dictionary.Add(DefaultFields.Path, contentPath);
            }

            if(content is IVersionable versionable)
            {
                dictionary.Add(DefaultFields.StartPublish, versionable.StartPublish);
                dictionary.Add(DefaultFields.StopPublish, versionable.StopPublish ?? DateTime.MaxValue);
            }

            if(content is IChangeTrackable trackable)
            {
                dictionary.Add(DefaultFields.Created, trackable.Created);
                dictionary.Add(DefaultFields.Changed, trackable.Changed);
            }

            if(content is ISecurable securable && securable.GetSecurityDescriptor() is IContentSecurityDescriptor acl)
            {
                var entries = acl.Entries.Select(a => $"{a.EntityType.ToString()[0]}:{a.Name}");
                dictionary.Add(DefaultFields.Acl, entries);
            }
        }

        private static Type GetContentType(IContent content)
        {
            Type contentType = content.GetUnproxiedType();
            if(contentType?.FullName?.StartsWith("Castle.") ?? false)
            {
                contentType = ProxyUtil.GetUnproxiedInstance(content).GetType().BaseType;
            }

            return contentType;
        }

        private static void AppendIndexableProperties(dynamic indexItem, IContent content, Type contentType, IDictionary<string, object> dictionary)
        {
            TryAddLanguageProperty(indexItem, content, dictionary, out CultureInfo language);

            List<PropertyInfo> indexableProperties = contentType.GetIndexableProps(false);
            bool ignoreXhtmlStringContentFragments = ElasticSearchSettings.IgnoreXhtmlStringContentFragments;

            indexableProperties.ForEach(property =>
            {
                if(!dictionary.ContainsKey(property.Name))
                {
                    object indexValue = GetIndexValue(content, property, out bool isString, ignoreXhtmlStringContentFragments);
                    if(indexValue != null)
                    {
                        dictionary.Add(property.Name, indexValue);
                    }
                }
            });

            AppendSuggestions(indexItem, content, contentType, indexableProperties);
        }

        private static void AppendSuggestions(dynamic indexItem, IContent content, Type contentType, List<PropertyInfo> indexableProperties)
        {
            List<Suggestion> suggestions = Indexing.Suggestions
                .Where(s => s.Type.IsAssignableFrom(contentType))
                .ToList();

            Logger.Debug($"Found {suggestions.Count} suggestions");

            if(suggestions.Count > 0)
            {
                var suggestionItems = new IndexItem.SuggestionItem();

                foreach(Suggestion suggestion in suggestions)
                {
                    Logger.Debug(
                        $"Type: {suggestion.Type}, All: {suggestion.IncludeAllFields}, InputFields: {String.Join(", ", suggestion.InputFields)}");

                    List<PropertyInfo> suggestProperties = indexableProperties;

                    if(!suggestion.IncludeAllFields)
                    {
                        suggestProperties = suggestProperties
                            .Where(
                                s => suggestion.InputFields.Contains(s.Name)
                                     || suggestion.InputFields.Contains("Page" + s.Name)) // builtin Epi property
                            .ToList();
                    }

                    char[] trimChars = { ',', '.', '/', ' ', ':', ';', '!', '?', '\"', '(', ')' };


                    var newSuggestions = suggestProperties
                        .Select(p =>
                        {
                            var value = GetIndexValue(content, p);
                            return value?.ToString();
                        })
                        .Where(p => p != null)
                        .Select(s=>s.ToLowerInvariant().Trim(trimChars))
                        .Where(v => !String.IsNullOrWhiteSpace(v) && (!TextUtil.IsNumeric(v) && v.Length > 1))
                        .Distinct()
                        .ToArray();

                    suggestionItems.Input = newSuggestions;
                }

                indexItem.Suggest = suggestionItems;
            }
        }

        private static object SerializeValue(object value)
        {
            if(value == null)
            {
                return null;
            }

            return Serialization.Serialize(value);
        }

        private static List<CustomProperty> GetCustomPropertiesForType(Type contentType)
        {
            return Indexing.CustomProperties
                .Where(c => c.OwnerType == contentType || c.OwnerType.IsAssignableFrom(contentType))
                .ToList();
        }

        private static object GetIndexValue(IContentData content, PropertyInfo p, bool ignoreXhtmlStringContentFragments = false, List<IContent> alreadyProcessedContent = null)
            => GetIndexValue(content, p, out _, ignoreXhtmlStringContentFragments, alreadyProcessedContent);

        private static object GetIndexValue(IContentData content, PropertyInfo p, out bool isString, bool ignoreXhtmlStringContentFragments = false, List<IContent> alreadyProcessedContent = null)
        {
            isString = false;

            // Being a chicken here.
            try
            {
                object value = p.GetValue(content);
                if(value == null)
                {
                    return null;
                }

                // Store IEnumerables as arrays
                if(ArrayHelper.IsArrayCandidate(p))
                {
                    return ArrayHelper.ToArray(value);
                }

                if(value is string)
                {
                    isString = true;
                    return TextUtil.StripHtmlAndEntities(value.ToString());
                }

                if(value is ContentArea contentArea)
                {
                    var indexText = new StringBuilder();

                    if(alreadyProcessedContent == null)
                    {
                        alreadyProcessedContent = new List<IContent>();
                    }

                    foreach(ContentAreaItem item in contentArea.FilteredItems)
                    {
                        IContent areaItemContent = item.GetContent();

                        if(Indexer.IsExcludedType(areaItemContent) || alreadyProcessedContent.Contains(areaItemContent))
                        {
                            continue;
                        }

                        Type areaItemType = GetContentType(areaItemContent);
                        List<PropertyInfo> indexableProperties = areaItemType.GetIndexableProps(false);
                        alreadyProcessedContent.Add(areaItemContent);
                        indexableProperties.ForEach(property =>
                        {
                            var indexValue = GetIndexValue(areaItemContent, property, alreadyProcessedContent: alreadyProcessedContent);
                            indexText.Append(indexValue);
                            indexText.Append(" ");
                        });
                    }

                    return indexText.ToString();
                }

                if(value is XhtmlString xhtml)
                {
                    isString = true;
                    var indexText = new StringBuilder(TextUtil.StripHtml(value.ToString()));

                    IPrincipal principal = HostingEnvironment.IsHosted
                        ? PrincipalInfo.AnonymousPrincipal
                        : null;

                    // Avoid infinite loop
                    // occurs when a page A have another page B in XhtmlString and page B as well have page A in XhtmlString
                    // or if page A have another page B in XhtmlString, page B have another page C in XhtmlString and page C have page A in XhtmlString
                    if(ignoreXhtmlStringContentFragments)
                    {
                        return indexText.ToString();
                    }

                    foreach(ContentFragment fragment in xhtml.GetFragments(principal))
                    {
                        if(IsValidFragment(fragment, out var fragmentContent))
                        {
                            Type fragmentType = GetContentType(fragmentContent);

                            List<PropertyInfo> indexableProperties = fragmentType.GetIndexableProps(false);
                            indexableProperties.ForEach(property =>
                            {
                                var indexValue = GetIndexValue(fragmentContent, property, ignoreXhtmlStringContentFragments: true);
                                indexText.Append(indexValue);
                                indexText.Append(" ");
                            });
                        }
                    }

                    return indexText.ToString();
                }

                if(p.PropertyType.IsEnum)
                {
                    return (int)value;
                }

                // Local block
                if(typeof(BlockData).IsAssignableFrom(p.PropertyType))
                {
                    var flattenedValue = new StringBuilder();
                    foreach(PropertyInfo prop in p.PropertyType.GetIndexableProps(false))
                    {
                        flattenedValue.Append(GetIndexValue(value as IContentData, prop));
                        flattenedValue.Append(" ");
                    }
                    return flattenedValue.ToString();
                }

                return value;
            }
            catch(Exception ex)
            {
                Logger.Warning($"GetIndexValue failed for content with id '{(content as IContent)?.ContentLink}'", ex);
                return null;
            }
        }

        private static bool IsValidFragment(ContentFragment fragment, out IContent fragmentContent)
        {
            fragmentContent = null;

            return fragment.ContentLink != null
                && fragment.ContentLink != ContentReference.EmptyReference
                && ContentLoader.TryGet(fragment.ContentLink, out fragmentContent)
                && fragmentContent != null
                && !Indexer.IsExcludedType(fragmentContent);
        }

        private static string GetAttachmentData(IContent content, out bool extensionNotAllowed)
        {
            extensionNotAllowed = false;

            if(!ElasticSearchSettings.EnableFileIndexing)
            {
                return null;
            }

            try
            {
                if(content is MediaData mediaData)
                {
                    string extension = Path.GetExtension(mediaData.RouteSegment ?? String.Empty).Trim(' ', '.');

                    if(!Indexing.IncludedFileExtensions.Contains(extension.ToLower()))
                    {
                        extensionNotAllowed = true;
                        return null;
                    }

                    if(ElasticSearchSettings.DisableContentIndexing)
                    {
                        Logger.Information($"Content indexing disabled for '{extension}'");
                        return String.Empty;
                    }

                    if(IsBinary(extension))
                    {
                        Logger.Information($"Extension '{extension}' is a binary type, skipping its contents");
                        return String.Empty;
                    }

                    using(var memoryStream = new MemoryStream())
                    {
                        using(var stream = mediaData.BinaryData.OpenRead())
                        {
                            stream.CopyTo(memoryStream);
                            var size = memoryStream.Length;
                            if(BlobIsTooLarge(size))
                            {
                                Logger.Warning($"MediaData '{content.Name} (ID: {content.ContentLink})' has size {size:n0} and is larger than the configured maxsize {ElasticSearchSettings.DocumentMaxSize:n0} bytes");
                                return String.Empty;
                            }

                            return Convert.ToBase64String(memoryStream.ToArray());
                        }
                    }
                }
            }
            catch(Exception)
            {
                Logger.Warning($"Failed to index MediaData '{content.Name} (ID: {content.ContentLink})'");
                extensionNotAllowed = true;
                return null;
            }

            return null;
        }

        internal static bool IsBinary(string fileExtension)
        {
            return !String.IsNullOrWhiteSpace(fileExtension)
                && BinaryExtensions.Contains(fileExtension.TrimStart('.').ToLowerInvariant());
        }

        private static string GetIndexName<T>(IElasticSearchService<T> service)
        {
            if(!String.IsNullOrEmpty(service.IndexName))
            {
                return service.IndexName;
            }

            return ElasticSearchSettings.GetDefaultIndexName(service.SearchLanguage);
        }

        private static bool BlobIsTooLarge(in long bytes)
        {
            if(ElasticSearchSettings.DocumentMaxSize <= 0)
            {
                return true;
            }

            return bytes >= ElasticSearchSettings.DocumentMaxSize;
        }
    }
}
