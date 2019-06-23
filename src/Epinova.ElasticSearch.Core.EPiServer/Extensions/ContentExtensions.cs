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
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Providers;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.DataAccess.Internal;
using EPiServer.Filters;
using EPiServer.Framework.Blobs;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        private static readonly string[] BinaryExtensions = new[]
        {
            "jpg", "jpeg", "gif", "psd", "bmp", "ai", "webp", "tif", "tiff", "ico", "jif", "png", "xcf", "eps", "raw", "cr2", "pct", "bpg",
            "exe", "zip", "rar", "7z", "dll", "gz", "bin", "iso", "apk", "dmp", "msi",
            "mp4", "mkv", "avi", "mov", "mpg", "mpeg", "vob", "flv", "h264", "m4v", "swf", "wmv",
            "mp3", "aac", "wav", "flac", "ogg", "mka", "wma", "aif", "mpa"
        };

        public static async Task<ContentSearchResult<T>> GetContentResultsAsync<T>(
            this IElasticSearchService<T> service,
            bool requirePageTemplate = false, string[] providerNames = null) where T : IContentData
        {
            return await GetContentResultsAsync(service, CancellationToken.None, requirePageTemplate, providerNames).ConfigureAwait(false);
        }

        public static async Task<ContentSearchResult<T>> GetContentResultsAsync<T>(
            this IElasticSearchService<T> service,
            CancellationToken cancellationToken,
            bool requirePageTemplate = false,
            string[] providerNames = null) where T : IContentData
        {
            SearchResult results = await service.GetResultsAsync(cancellationToken, DefaultFields.Id).ConfigureAwait(false);

            IEnumerable<Task<ContentSearchHit<T>>> tasks =
                results.Hits.Select(h => FilterAsync<T>(h, requirePageTemplate, providerNames));

            ContentSearchHit<T>[] hits = await Task.WhenAll(tasks).ConfigureAwait(false);
            hits = hits.Where(h => h != null).ToArray();

            results.TotalHits = hits.Length;

            if (service.TrackSearch)
            {
                TrackingRepository.AddSearch(
                    Language.GetLanguageCode(service.SearchLanguage),
                    service.SearchText,
                    results.TotalHits == 0,
                    GetIndexName(service));
            }

            return new ContentSearchResult<T>(results, hits);
        }

        public static ContentSearchResult<T> GetContentResults<T>(
            this IElasticSearchService<T> service) where T : IContentData
        {
            return service.GetContentResults(false, null);
        }

        public static ContentSearchResult<T> GetContentResults<T>(
            this IElasticSearchService<T> service,
            bool requirePageTemplate) where T : IContentData
        {
            return service.GetContentResults(requirePageTemplate, null);
        }

        public static ContentSearchResult<T> GetContentResults<T>(
            this IElasticSearchService<T> service,
            bool requirePageTemplate,
            string[] providerNames) where T : IContentData
        {
            return service.GetContentResults(requirePageTemplate, false, providerNames, true, true);
        }

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

            foreach (SearchHit hit in results.Hits)
            {
                if (ShouldAdd(hit, requirePageTemplate, out T content, providerNames, ignoreFilters))
                {
                    hits.Add(new ContentSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight));
                }
                else
                {
                    results.TotalHits--;
                }
            }

            if (service.TrackSearch)
            {
                TrackingRepository.AddSearch(
                    Language.GetLanguageCode(service.SearchLanguage),
                    service.SearchText,
                    results.TotalHits == 0,
                    GetIndexName(service));
            }

            return new ContentSearchResult<T>(results, hits);
        }

        private static async Task<ContentSearchHit<T>> FilterAsync<T>(SearchHit hit, bool requirePageTemplate, string[] providerNames) where T : IContentData
        {
            return await Task.Run(() =>
            {
                if (!ShouldAdd(hit, requirePageTemplate, out T content, providerNames, false))
                {
                    return null;
                }

                return new ContentSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight);
            }).ConfigureAwait(false);
        }

        internal static bool ShouldAdd<T>(this SearchHit hit, bool requirePageTemplate, out T content, string[] providerNames, bool ignoreFilters)
            where T : IContentData
        {
            if (providerNames?.Any() != true)
            {
                var contentLink = new ContentReference(hit.Id);

                if (!String.IsNullOrEmpty(hit.Lang))
                {
                    ContentLoader.TryGet(contentLink, CultureInfo.GetCultureInfo(hit.Lang), out content);
                }
                else
                {
                    ContentLoader.TryGet(contentLink, out content);
                }
            }
            else
            {
                content = GetContentForProviders<T>(hit, providerNames);
            }

            return content != null && !ShouldFilter(content as IContent, requirePageTemplate, ignoreFilters);
        }

        private static T GetContentForProviders<T>(SearchHit hit, string[] providerNames) where T : IContentData
        {
            foreach (var name in providerNames)
            {
                ContentReference contentLink =
                    new[] { ProviderConstants.CmsProviderKey, ProviderConstants.DefaultProviderKey }.Contains(name)
                        ? new ContentReference(hit.Id)
                        : new ContentReference(hit.Id, 0, name);

                if (!String.IsNullOrEmpty(hit.Lang))
                {
                    if (ContentLoader.TryGet(contentLink, CultureInfo.GetCultureInfo(hit.Lang), out T content))
                    {
                        return content;
                    }
                }
                else
                {
                    if (ContentLoader.TryGet(contentLink, out T content))
                    {
                        return content;
                    }
                }
            }

            return default;
        }

        private static bool ShouldFilter(IContent content, bool requirePageTemplate, bool ignoreFilters)
        {
            if (content == null)
            {
                return true;
            }

            if (ignoreFilters)
            {
                return false;
            }

            if (Indexer.ShouldHideFromSearch(content))
            {
                return true;
            }

            var accessFilter = new FilterAccess();
            var publishedFilter = new FilterPublished();

            if (publishedFilter.ShouldFilter(content) || accessFilter.ShouldFilter(content))
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
            if (contentLink.ProviderName == ProviderConstants.CatalogProviderKey)
            {
                return GetPathTheHardWay(contentLink);
            }

            if (contentPath?.Any() != true)
            {
                return null;
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
            while (current != null)
            {
                path.Add(current.ID);

                if (ContentLoader.TryGet(current, out IContent content))
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

            if (!TryAddAttachmentData(content, dictionary))
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

            foreach (CustomProperty property in customProperties)
            {
                try
                {
                    object value;
                    Type returnType = property.Getter.Method.ReturnType;
                    bool isArrayCandidate = ArrayHelper.IsArrayCandidate(returnType);
                    bool isDictionary = ArrayHelper.IsDictionary(returnType);

                    // Set returnType to underlying type for nullable value-types
                    if (returnType.IsValueType && returnType.IsGenericType && returnType.GenericTypeArguments.Length > 0)
                    {
                        returnType = returnType.GenericTypeArguments[0];
                    }

                    if (returnType.IsValueType && !Mapping.IsNumericType(returnType))
                    {
                        value = returnType == typeof(bool)
                            ? SerializeValue(property.Getter.DynamicInvoke(content))
                            : SerializeValue(property.Getter.DynamicInvoke());
                    }
                    else if (isDictionary)
                    {
                        value = ArrayHelper.ToDictionary(property.Getter.DynamicInvoke(content));
                    }
                    else if (isArrayCandidate)
                    {
                        value = ArrayHelper.ToArray(property.Getter.DynamicInvoke(content));

                        if (value is IEnumerable<object> arrayValue && !arrayValue.Any())
                        {
                            Logger.Debug($"Value for '{property.Name}' is empty array value, skipping");
                            continue;
                        }
                    }
                    else
                    {
                        value = SerializeValue(property.Getter.DynamicInvoke(content));
                    }

                    if (returnType == typeof(CategoryList))
                    {
                        returnType = typeof(int[]);
                    }

                    if (value != null)
                    {
                        if (!isArrayCandidate && !isDictionary)
                        {
                            Logger.Debug($"Changing type of value '{value}' to '{returnType.Name}'");
                            value = Convert.ChangeType(value, returnType, CultureInfo.InvariantCulture);

                            if (returnType.IsAssignableFrom(typeof(String)))
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
                catch (Exception ex)
                {
                    Logger.Error("Failed to index custom property: " + property.Name, ex);
                }
            }
        }

        private static void TryAddLanguageProperty(dynamic indexItem, IContent content, IDictionary<string, object> dictionary, out string language)
        {
            language = null;
            if (content is ILocale locale && locale.Language != null && !CultureInfo.InvariantCulture.Equals(locale.Language))
            {
                language = locale.Language.Name;
                dictionary.Add(DefaultFields.Lang, language);
                if (Logger.IsDebugEnabled())
                {
                    string lang = indexItem.Lang.ToString();
                    Logger.Debug("Language: " + lang);
                }

                IBestBetsRepository repository = ServiceLocator.Current.GetInstance<IBestBetsRepository>();

                dictionary.Add(DefaultFields.BestBets, repository.GetBestBetsForContent(language, content.ContentLink.ID, null));
            }
        }

        private static bool TryAddAttachmentData(IContent content, IDictionary<string, object> dictionary)
        {
            string attachmentData = GetAttachmentData(content, out bool extensionNotAllowed);
            if (extensionNotAllowed)
            {
                return false;
            }

            if (attachmentData != null)
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
            if (content.ParentLink != null)
            {
                dictionary.Add(DefaultFields.ParentLink, content.ParentLink.ID);
            }

            dictionary.Add(DefaultFields.Name, content.Name);
            dictionary.Add(DefaultFields.Type, typeName);
            dictionary.Add(DefaultFields.Types, contentType.GetInheritancHierarchyArray());

            int[] contentPath = GetContentPath(content.ContentLink);
            if (contentPath != null)
            {
                dictionary.Add(DefaultFields.Path, contentPath);
            }

            if (content is IVersionable versionable)
            {
                dictionary.Add(DefaultFields.StartPublish, versionable.StartPublish);
                dictionary.Add(DefaultFields.StopPublish, versionable.StopPublish ?? DateTime.MaxValue);
            }

            if (content is IChangeTrackable trackable)
            {
                dictionary.Add(DefaultFields.Created, trackable.Created);
                dictionary.Add(DefaultFields.Changed, trackable.Changed);
            }

            if (content is ISecurable securable && securable.GetSecurityDescriptor() is IContentSecurityDescriptor acl)
            {
                var entries = acl.Entries.Select(a => $"{a.EntityType.ToString()[0]}:{a.Name}");
                dictionary.Add(DefaultFields.Acl, entries);
            }
        }

        private static Type GetContentType(IContent content)
        {
            Type contentType = content.GetUnproxiedType();
            if (contentType?.FullName?.StartsWith("Castle.") ?? false)
            {
                contentType = ProxyUtil.GetUnproxiedInstance(content).GetType().BaseType;
            }

            return contentType;
        }

        private static void AppendIndexableProperties(dynamic indexItem, IContent content, Type contentType, IDictionary<string, object> dictionary)
        {
            TryAddLanguageProperty(indexItem, content, dictionary, out string language);

            List<PropertyInfo> indexableProperties = contentType.GetIndexableProps(false);
            bool ignoreXhtmlStringContentFragments = ElasticSearchSettings.IgnoreXhtmlStringContentFragments;

            indexableProperties.ForEach(property =>
            {
                if (!dictionary.ContainsKey(property.Name))
                {
                    object indexValue = GetIndexValue(content, property, out bool isString, ignoreXhtmlStringContentFragments);
                    if (indexValue != null)
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

            if (suggestions.Count > 0)
            {
                var suggestionItems = new IndexItem.SuggestionItem();

                foreach (Suggestion suggestion in suggestions)
                {
                    Logger.Debug(
                        $"Type: {suggestion.Type}, All: {suggestion.IncludeAllFields}, InputFields: {String.Join(", ", suggestion.InputFields)}");

                    List<PropertyInfo> suggestProperties = indexableProperties;

                    if (!suggestion.IncludeAllFields)
                    {
                        suggestProperties = suggestProperties
                            .Where(
                                s => suggestion.InputFields.Contains(s.Name)
                                     || suggestion.InputFields.Contains("Page" + s.Name)) // builtin Epi property
                            .ToList();
                    }

                    char[] trimChars = { ',', '.', '/', ' ', ':', ';', '!', '?', '\"', '(', ')' };

                    suggestionItems.Input = String.Join(" ",
                            suggestProperties
                                .Select(p =>
                                {
                                    var value = GetIndexValue(content, p);
                                    return value?.ToString();
                                })
                                .Where(p => p != null)
                        )
                        .ToLowerInvariant()
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim(trimChars))
                        .Where(v => !String.IsNullOrWhiteSpace(v) && (!TextUtil.IsNumeric(v) && v.Length > 1))
                        .Distinct()
                        .ToArray();
                }

                indexItem.Suggest = suggestionItems;
            }
        }

        private static object SerializeValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            return Serialization.Serialize(value);
        }

        internal static void CreateDidYouMeanMappingsIfNeeded(Type type, string language, string indexName = null)
        {
            Logger.Debug("Checking if DidYouMean mappings needs updating");

            var stringProperties = type.GetIndexableProps(false)
                .Where(p => p.PropertyType == typeof(string) || p.PropertyType == typeof(string[]) || p.PropertyType == typeof(XhtmlString))
                .Select(p => p.Name)
                .ToList();

            string json = null;
            string oldJson = null;

            try
            {
                IndexMapping mapping = Mapping.GetIndexMapping(typeof(IndexItem), language, indexName);

                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                oldJson = JsonConvert.SerializeObject(mapping, jsonSettings);

                IEnumerable<string> filtered = stringProperties
                    .Except(WellKnownProperties.IgnoreDidYouMean)
                    .Except(WellKnownProperties.Ignore);

                foreach (string prop in filtered)
                {
                    mapping.Properties.TryGetValue(prop, out IndexMappingProperty property);
                    mapping.AddOrUpdateProperty(prop, property);
                }

                if (!mapping.IsDirty)
                {
                    // No change, quit.
                    Logger.Debug("No change");
                    return;
                }

                json = JsonConvert.SerializeObject(mapping, jsonSettings);
                var data = Encoding.UTF8.GetBytes(json);

                if (String.IsNullOrWhiteSpace(indexName))
                {
                    indexName = ElasticSearchSettings.GetDefaultIndexName(language);
                }

                var uri = $"{ElasticSearchSettings.Host}/{indexName}/_mapping/{typeof(IndexItem).GetTypeName()}";
                if (Server.Info.Version.Major >= 7)
                {
                    uri += "?include_type_name=true";
                }

                Logger.Debug("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

                HttpClientHelper.Put(new Uri(uri), data);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update mappings", ex);

                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Old mapping:\n" + JToken.Parse(oldJson ?? String.Empty).ToString(Formatting.Indented));
                    Logger.Debug("New mapping:\n" + JToken.Parse(json ?? String.Empty).ToString(Formatting.Indented));
                }
            }
        }

        private static List<CustomProperty> GetCustomPropertiesForType(Type contentType)
        {
            return Indexing.CustomProperties
                .Where(c => c.OwnerType == contentType || c.OwnerType.IsAssignableFrom(contentType))
                .ToList();
        }

        internal static void CreateAnalyzedMappingsIfNeeded(Type type, string language, string indexName = null)
        {
            Logger.Debug("Checking if analyzable mappings needs updating");

            string json = null;
            string oldJson = null;
            IndexMapping mapping = null;

            try
            {
                if (String.IsNullOrWhiteSpace(indexName))
                {
                    indexName = ElasticSearchSettings.GetDefaultIndexName(language);
                }

                // Get mappings from server
                mapping = Mapping.GetIndexMapping(typeof(IndexItem), language, indexName);

                // Ignore special mappings
                mapping.Properties.Remove(DefaultFields.AttachmentData);
                mapping.Properties.Remove(DefaultFields.BestBets);
                mapping.Properties.Remove(DefaultFields.DidYouMean);
                mapping.Properties.Remove(DefaultFields.Suggest);
                mapping.Properties.Remove(nameof(IndexItem.attachment));

                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                oldJson = JsonConvert.SerializeObject(mapping, jsonSettings);

                // Get indexable properties (string, XhtmlString, [Searchable(true)]) 
                var indexableProperties = type.GetIndexableProps(false)
                    .Select(p => new
                    {
                        p.Name,
                        Type = p.PropertyType,
                        Analyzable = ((p.PropertyType == typeof(string) || p.PropertyType == typeof(string[]))
                                     && (p.GetCustomAttributes(typeof(StemAttribute)).Any() || WellKnownProperties.Analyze
                                          .Select(w => w.ToLower())
                                          .Contains(p.Name.ToLower())))
                                     || (p.PropertyType == typeof(XhtmlString)
                                     && p.GetCustomAttributes(typeof(ExcludeFromSearchAttribute), true).Length == 0)
                    })
                    .Where(p => p.Name != nameof(IndexItem.Type)
                                && p.Name != nameof(IndexItem._bestbets)
                                && p.Name != DefaultFields.DidYouMean
                                && p.Name != DefaultFields.Suggest
                                && p.Name != nameof(IndexItem.attachment)
                                && p.Name != nameof(IndexItem._attachmentdata))
                    .ToList();

                // Get well-known and Stemmed property-names
                List<string> allAnalyzableProperties = indexableProperties.Where(i => i.Analyzable)
                    .Select(i => i.Name)
                    .ToList();

                allAnalyzableProperties.ForEach(p =>
                {
                    IndexMappingProperty propertyMapping = Language.GetPropertyMapping(language, typeof(string), true);

                    mapping.AddOrUpdateProperty(p, propertyMapping);
                });

                if (!mapping.IsDirty)
                {
                    // No change, quit.
                    Logger.Debug("No change");
                    return;
                }

                json = JsonConvert.SerializeObject(mapping, jsonSettings);
                var data = Encoding.UTF8.GetBytes(json);
                var uri = $"{ElasticSearchSettings.Host}/{indexName}/_mapping/{typeof(IndexItem).GetTypeName()}";
                if (Server.Info.Version.Major >= 7)
                {
                    uri += "?include_type_name=true";
                }

                Logger.Debug("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

                HttpClientHelper.Put(new Uri(uri), data);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to update mappings for content of type '{type.Name}'\n. Properties with the same name but different type, " +
                    "where one of the types is analyzable and the other is not, is often the cause of this error. Ie. 'string MainIntro' vs 'XhtmlString MainIntro'. \n" +
                    "All properties with equal name must be of the same type or ignored from indexing with [Searchable(false)]. \n" +
                    "Enable debug-logging to view further details.", ex);

                if (Logger.IsDebugEnabled())
                {
                    Logger.Debug("Old mapping:\n" + JToken.Parse(oldJson ?? String.Empty).ToString(Formatting.Indented));
                    Logger.Debug("New mapping:\n" + JToken.Parse(json ?? String.Empty).ToString(Formatting.Indented));

                    try
                    {
                        IndexMapping oldMappings = JsonConvert.DeserializeObject<IndexMapping>(oldJson);

                        foreach (KeyValuePair<string, IndexMappingProperty> oldMapping in oldMappings.Properties)
                        {
                            if (mapping?.Properties.ContainsKey(oldMapping.Key) == true
                                && !oldMapping.Value.Equals(mapping.Properties[oldMapping.Key]))
                            {
                                Logger.Error("Property '" + oldMapping.Key + "' has different mapping across different types");
                                Logger.Debug("Old: \n" + JsonConvert.SerializeObject(oldMapping.Value));
                                Logger.Debug("New: \n" + JsonConvert.SerializeObject(mapping.Properties[oldMapping.Key]));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to compare mappings", e);
                    }
                }
            }
        }

        private static object GetIndexValue(IContentData content, PropertyInfo p, bool ignoreXhtmlStringContentFragments = false)
        {
            return GetIndexValue(content, p, out _, ignoreXhtmlStringContentFragments);
        }

        private static object GetIndexValue(IContentData content, PropertyInfo p, out bool isString, bool ignoreXhtmlStringContentFragments = false)
        {
            isString = false;
            string id = null;
            string name = null;
            string type = null;

            // Being a chicken here.
            try
            {
                type = content.GetType().Name;

                if (content is IContent icontent)
                {
                    id = icontent.ContentLink.ID.ToString();
                    name = icontent.Name;
                }

                object value = p.GetValue(content);
                if (value == null)
                {
                    return null;
                }

                // Store IEnumerables as arrays
                if (ArrayHelper.IsArrayCandidate(p))
                {
                    return ArrayHelper.ToArray(value);
                }

                if (value is string)
                {
                    isString = true;
                    return TextUtil.StripHtmlAndEntities(value.ToString());
                }

                if (value is ContentArea contentArea)
                {
                    var indexText = new StringBuilder();

                    foreach (ContentAreaItem item in contentArea.FilteredItems)
                    {
                        IContent areaItemContent = item.GetContent();

                        if (Indexer.IsExludedType(areaItemContent))
                        {
                            continue;
                        }

                        Type areaItemType = GetContentType(areaItemContent);
                        List<PropertyInfo> indexableProperties = areaItemType.GetIndexableProps(false);
                        indexableProperties.ForEach(property =>
                        {
                            var indexValue = GetIndexValue(areaItemContent, property);
                            indexText.Append(indexValue);
                            indexText.Append(" ");
                        });
                    }

                    return indexText.ToString();
                }

                if (value is XhtmlString xhtml)
                {
                    isString = true;
                    var indexText = new StringBuilder(TextUtil.StripHtml(value.ToString()));

                    IPrincipal principal = HostingEnvironment.IsHosted
                        ? PrincipalInfo.AnonymousPrincipal
                        : null;

                    // Avoid infinite loop
                    // occurs when a page A have another page B in XhtmlString and page B as well have page A in XhtmlString
                    // or if page A have another page B in XhtmlString, page B have another page C in XhtmlString and page C have page A in XhtmlString
                    if (ignoreXhtmlStringContentFragments)
                    {
                        return indexText.ToString();
                    }

                    foreach (ContentFragment fragment in xhtml.GetFragments(principal))
                    {
                        if (fragment.ContentLink != null && fragment.ContentLink != ContentReference.EmptyReference)
                        {
                            if (ContentLoader.TryGet(fragment.ContentLink, out IContent fragmentContent) && !Indexer.IsExludedType(fragmentContent))
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
                    }

                    return indexText.ToString();
                }

                if (p.PropertyType.IsEnum)
                {
                    return (int)value;
                }

                // Local block
                if (typeof(BlockData).IsAssignableFrom(p.PropertyType))
                {
                    var flattenedValue = new StringBuilder();
                    foreach (PropertyInfo prop in p.PropertyType.GetIndexableProps(false))
                    {
                        flattenedValue.Append(GetIndexValue(value as IContentData, prop));
                        flattenedValue.Append(" ");
                    }
                    return flattenedValue.ToString();
                }

                return value;
            }
            catch (Exception ex)
            {
                Logger.Warning($"GetIndexValue failed for id '{id}'. Type={type}, Name={name}", ex);
                return null;
            }
        }

        private static string GetAttachmentData(IContent content, out bool extensionNotAllowed)
        {
            extensionNotAllowed = false;

            if (!ElasticSearchSettings.EnableFileIndexing)
            {
                return null;
            }

            string filePath = null;

            try
            {
                if (content is MediaData mediaData && mediaData.BinaryData is FileBlob fileBlob)
                {
                    filePath = fileBlob.FilePath;
                    if (SizeIsValid(fileBlob))
                    {
                        string extension = Path.GetExtension(fileBlob.FilePath ?? String.Empty).Trim(' ', '.');
                        if (!Indexing.IncludedFileExtensions.Contains(extension.ToLower()))
                        {
                            extensionNotAllowed = true;
                            return null;
                        }

                        if (IsBinary(extension))
                        {
                            Logger.Information($"Extension '{extension}' is a binary type, skipping its contents");
                            return String.Empty;
                        }

                        using (var memoryStream = new MemoryStream())
                        {
                            fileBlob.OpenRead()
                                .CopyTo(memoryStream);
                            return Convert.ToBase64String(memoryStream.ToArray());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Could not index MediaData with id {content.ContentLink}. The blob is probably missing on disk. Path: {filePath}");
                Logger.Debug("Details", ex);
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
            if (!String.IsNullOrEmpty(service.IndexName))
            {
                return service.IndexName;
            }

            return ElasticSearchSettings.GetDefaultIndexName(Language.GetLanguageCode(service.SearchLanguage));
        }

        private static bool SizeIsValid(FileBlob fileBlob)
        {
            return ElasticSearchSettings.DocumentMaxSize <= 0
                || new FileInfo(fileBlob.FilePath).Length <= ElasticSearchSettings.DocumentMaxSize;
        }
    }
}
