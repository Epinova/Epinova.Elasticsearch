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
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.Extensions;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;
using EPiServer.Filters;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Indexing = Epinova.ElasticSearch.Core.Conventions.Indexing;
using Castle.DynamicProxy;
using Epinova.ElasticSearch.Core.EPiServer.Providers;
using EPiServer.DataAccess.Internal;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    //TODO: Refactor, this is not an extension class anymore
    public static class ContentExtensions
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(ContentExtensions));
        private static readonly IContentLoader ContentLoader;
        private static readonly IElasticSearchSettings ElasticSearchSettings;
        private static readonly ITrackingRepository TrackingRepository;

        private static readonly string[] binaryExtensions = new[]
        {
            "jpg", "jpeg", "gif", "psd", "bmp", "ai", "webp", "tif", "tiff", "ico", "jif", "png", "xcf", "eps", "raw", "cr2", "pct", "bpg",
            "exe", "zip", "rar", "7z", "dll", "gz", "bin", "iso", "apk", "dmp", "msi",
            "mp4", "mkv", "avi", "mov", "mpg", "mpeg", "vob", "flv", "h264", "m4v", "swf", "wmv",
            "mp3", "aac", "wav", "flac", "ogg", "mka", "wma", "aif", "mpa"
        };

        static ContentExtensions()
        {
            ContentLoader = ServiceLocator.Current.GetInstance<IContentLoader>();
            ElasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            TrackingRepository = ServiceLocator.Current.GetInstance<ITrackingRepository>();
        }


        public static async Task<ContentSearchResult<T>> GetContentResultsAsync<T>(
            this IElasticSearchService<T> service,
            bool requirePageTemplate = false, string[] providerNames = null) where T : IContentData
        {
            return await GetContentResultsAsync(service, CancellationToken.None, requirePageTemplate, providerNames);
        }


        public static async Task<ContentSearchResult<T>> GetContentResultsAsync<T>(
            this IElasticSearchService<T> service,
            CancellationToken cancellationToken,
            bool requirePageTemplate = false,
            string[] providerNames = null) where T : IContentData
        {
            SearchResult results = await service.GetResultsAsync(cancellationToken, DefaultFields.Id);

            IEnumerable<Task<ContentSearchHit<T>>> tasks =
                results.Hits.Select(h => FilterAsync<T>(h, requirePageTemplate, providerNames));

            ContentSearchHit<T>[] hits = await Task.WhenAll(tasks);
            hits = hits.Where(h => h != null).ToArray();

            results.TotalHits = hits.Length;

            if (service.TrackSearch)
            {
                TrackingRepository.AddSearch(
                    Language.GetLanguageCode(service.SearchLanguage),
                    service.SearchText,
                    results.TotalHits == 0);
            }

            return new ContentSearchResult<T>(results, hits);
        }


        public static ContentSearchResult<T> GetContentResults<T>(
            this IElasticSearchService<T> service,
            bool requirePageTemplate = false,
            string[] providerNames = null) where T : IContentData
        {
            SearchResult results = service.GetResults();
            var hits = new List<ContentSearchHit<T>>();

            foreach (SearchHit hit in results.Hits)
            {
                if (ShouldAdd(hit, requirePageTemplate, out T content, providerNames))
                    hits.Add(new ContentSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight));
                else
                    results.TotalHits--;
            }

            if (service.TrackSearch)
            {
                TrackingRepository.AddSearch(
                    Language.GetLanguageCode(service.SearchLanguage),
                    service.SearchText,
                    results.TotalHits == 0);
            }

            return new ContentSearchResult<T>(results, hits);
        }

        private static async Task<ContentSearchHit<T>> FilterAsync<T>(SearchHit hit, bool requirePageTemplate, string[] providerNames) where T : IContentData
        {
            return await Task.Run(() =>
            {
                if (!ShouldAdd(hit, requirePageTemplate, out T content, providerNames))
                    return null;

                return new ContentSearchHit<T>(content, hit.CustomProperties, hit.QueryScore, hit.Highlight);
            });
        }

        private static bool ShouldAdd<T>(SearchHit hit, bool requirePageTemplate, out T content, string[] providerNames)
            where T : IContentData
        {
            if (providerNames == null || !providerNames.Any())
            {
                ContentReference contentLink = new ContentReference(hit.Id);

                if (!string.IsNullOrEmpty(hit.Lang))
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

            return content != null && !ShouldFilter(content as IContent, requirePageTemplate);
        }

        private static T GetContentForProviders<T>(SearchHit hit, string[] providerNames) where T : IContentData
        {
            foreach (var name in providerNames)
            {
                ContentReference contentLink =
                    new[] { ProviderConstants.CmsProviderKey, ProviderConstants.DefaultProviderKey }.Contains(name)
                        ? new ContentReference(hit.Id)
                        : new ContentReference(hit.Id, 0, name);

                if (!string.IsNullOrEmpty(hit.Lang))
                {
                    if (ContentLoader.TryGet(contentLink, CultureInfo.GetCultureInfo(hit.Lang), out T content))
                        return content;
                }
                else
                {
                    if (ContentLoader.TryGet(contentLink, out T content))
                        return content;
                }
            }

            return default;
        }

        private static bool ShouldFilter(IContent content, bool requirePageTemplate)
        {
            if (content == null)
                return true;

            if (Indexer.ShouldHideFromSearch(content))
                return true;

            var accessFilter = new FilterAccess();
            var publishedFilter = new FilterPublished();

            if (publishedFilter.ShouldFilter(content) || accessFilter.ShouldFilter(content))
                return true;

            var templateFilter = new FilterTemplate();

            return requirePageTemplate && templateFilter.ShouldFilter(content);
        }

        internal static int[] GetContentPath(ContentReference contentLink)
        {
            ContentPath contentPath = ServiceLocator.Current.GetInstance<ContentPathDB>().GetContentPath(contentLink);

            // Commerce doesn't support ContentPath
            if (contentLink.ProviderName == ProviderConstants.CatalogProviderKey)
                return GetPathTheHardWay(contentLink);

            if (contentPath == null || !contentPath.Any())
                return null;

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
                    current = content.ParentLink;
                else
                    current = null;
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
                return null;

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
                        returnType = typeof(int[]);

                    if (value != null)
                    {
                        if (!isArrayCandidate)
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

                CreateAnalyzedMappingsIfNeeded(content, language);
            }
        }

        private static bool TryAddAttachmentData(IContent content, IDictionary<string, object> dictionary)
        {
            string attachmentData = GetAttachmentData(content, out bool extensionNotAllowed);
            if (extensionNotAllowed)
                return false;

            if (attachmentData != null)
                dictionary.Add(DefaultFields.Attachment, attachmentData);

            return true;
        }

        private static void AppendDefaultFields(IContent content, IDictionary<string, object> dictionary, Type contentType)
        {
            string typeName = contentType.GetTypeName();

            dictionary.Add(DefaultFields.Id, content.ContentLink.ID);
            dictionary.Add(DefaultFields.Indexed, DateTime.Now);
            if (content.ParentLink != null)
                dictionary.Add(DefaultFields.ParentLink, content.ParentLink.ID);
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
                dictionary.Add(DefaultFields.StopPublish, versionable.StopPublish.GetValueOrDefault(DateTime.MaxValue));
            }

            if (content is IChangeTrackable trackable)
            {
                dictionary.Add(DefaultFields.Created, trackable.Created);
                dictionary.Add(DefaultFields.Changed, trackable.Changed);
            }
        }

        private static Type GetContentType(IContent content)
        {
            Type contentType = content.GetUnproxiedType();
            if (contentType?.FullName?.StartsWith("Castle.") ?? false)
                contentType = ProxyUtil.GetUnproxiedInstance(content).GetType().BaseType;
            return contentType;
        }

        private static void AppendIndexableProperties(dynamic indexItem, IContent content, Type contentType, IDictionary<string, object> dictionary)
        {
            TryAddLanguageProperty(indexItem, content, dictionary, out string language);

            var stringProperties = new List<string>();
            List<PropertyInfo> indexableProperties = contentType.GetIndexableProps(false);
            bool ignoreXhtmlStringContentFragments = ElasticSearchSettings.IgnoreXhtmlStringContentFragments;

            indexableProperties.ForEach(property =>
            {
                object indexValue = GetIndexValue(content, property, out bool isString, ignoreXhtmlStringContentFragments);
                string propertyName = property.Name;
                if (isString && !stringProperties.Contains(propertyName))
                {
                    stringProperties.Add(propertyName);
                }

                if (!dictionary.ContainsKey(property.Name) && indexValue != null)
                    dictionary.Add(property.Name, indexValue);
            });

            if (language != null && stringProperties.Count > 0)
                CreateDidYouMeanMappingsIfNeeded(stringProperties, language);

            AppendSuggestions(indexItem, content, contentType, indexableProperties);
        }

        private static void AppendSuggestions(dynamic indexItem, IContent content, Type contentType, List<PropertyInfo> indexableProperties)
        {
            List<Suggestion> suggestions = Indexing.Suggestions
                .Where(s => s.Type.IsAssignableFrom(contentType))
                .ToList();

            Logger.Debug($"Found {suggestions.Count} suggestions");

            if (suggestions.Any())
            {
                IndexItem.SuggestionItem suggestionItems = new IndexItem.SuggestionItem();

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
                                    object value = GetIndexValue(content, p);
                                    return value?.ToString();
                                })
                                .Where(p => p != null)
                        )
                        .ToLowerInvariant()
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(v => v.Trim(trimChars))
                        .Where(v => !String.IsNullOrWhiteSpace(v) && !TextUtil.IsNumeric(v) & v.Length > 1)
                        .Distinct()
                        .ToArray();
                }

                indexItem.Suggest = suggestionItems;
            }
        }

        private static object SerializeValue(object value)
        {
            if (value == null)
                return null;

            return Serialization.Serialize(value);
        }

        private static void CreateDidYouMeanMappingsIfNeeded(List<string> stringProperties, string language, string indexName = null)
        {
            Logger.Debug("Checking if DidYouMean mappings needs updating");

            string json = null;
            string oldJson = null;

            try
            {
                IndexMapping mapping = Mapping.GetIndexMapping(typeof(IndexItem), language, null);

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
                byte[] data = Encoding.UTF8.GetBytes(json);

                if (String.IsNullOrWhiteSpace(indexName))
                    indexName = ElasticSearchSettings.GetDefaultIndexName(language);

                string uri = $"{ElasticSearchSettings.Host}/{indexName}/_mapping/{typeof(IndexItem).GetTypeName()}";

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

        private static void CreateAnalyzedMappingsIfNeeded(IContent content, string language, string indexName = null)
        {
            Logger.Debug("Checking if analyzable mappings needs updating");

            string json = null;
            string oldJson = null;
            IndexMapping mapping = null;

            try
            {
                if (String.IsNullOrWhiteSpace(indexName))
                    indexName = ElasticSearchSettings.GetDefaultIndexName(language);

                mapping = Mapping.GetIndexMapping(typeof(IndexItem), language, indexName);

                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                oldJson = JsonConvert.SerializeObject(mapping, jsonSettings);

                // Get indexable properties (string, XhtmlString, [Searchable(true)]) 
                var indexableProperties = content.GetType().GetIndexableProps(false)
                    .Select(p => new
                    {
                        p.Name,
                        Type = p.PropertyType,
                        Analyzable = (p.PropertyType == typeof(string) || p.PropertyType == typeof(string[])) &&
                                     (p.GetCustomAttributes(typeof(StemAttribute)).Any() || WellKnownProperties.Analyze
                                          .Select(w => w.ToLower())
                                          .Contains(p.Name.ToLower()))
                                     || p.PropertyType == typeof(XhtmlString) &&
                                     !p.GetCustomAttributes(typeof(ExcludeFromSearchAttribute), true).Any()
                    })
                    .ToList();

                // Get well-known and Stemmed property-names
                List<string> allAnalyzableProperties = indexableProperties.Where(i => i.Analyzable)
                    .Select(i => i.Name)
                    .ToList();

                allAnalyzableProperties.ForEach(p =>
                {
                    IndexMappingProperty propertyMapping =
                        Language.GetPropertyMapping(language, typeof(string), true);

                    mapping.AddOrUpdateProperty(p, propertyMapping);
                });

                if (!mapping.IsDirty)
                {
                    // No change, quit.
                    Logger.Debug("No change");
                    return;
                }

                json = JsonConvert.SerializeObject(mapping, jsonSettings);
                byte[] data = Encoding.UTF8.GetBytes(json);
                string uri = $"{ElasticSearchSettings.Host}/{indexName}/_mapping/{typeof(IndexItem).GetTypeName()}";

                Logger.Debug("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

                HttpClientHelper.Put(new Uri(uri), data);
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Failed to update mappings for content with id '{content.ContentLink}'\n. Properties with the same name but different type, " +
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
                            if (mapping != null && mapping.Properties.ContainsKey(oldMapping.Key) &&
                                !oldMapping.Value.Equals(mapping.Properties[oldMapping.Key]))
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
                    return null;

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

                if (value is ContentArea)
                {
                    string indexText = null;

                    foreach (ContentAreaItem item in (value as ContentArea).FilteredItems)
                    {
                        IContent areaItemContent = item.GetContent();

                        if (Indexer.IsExludedType(areaItemContent))
                            continue;

                        Type areaItemType = GetContentType(areaItemContent);
                        List<PropertyInfo> indexableProperties = areaItemType.GetIndexableProps(false);
                        indexableProperties.ForEach(property =>
                        {
                            object indexValue = GetIndexValue(areaItemContent, property);
                            indexText += " " + indexValue;
                        });
                    }

                    return indexText;
                }

                if (value is XhtmlString)
                {
                    isString = true;
                    XhtmlString xhtml = value as XhtmlString;
                    string indexText = TextUtil.StripHtmlAndEntities(value.ToString());

                    IPrincipal principal = HostingEnvironment.IsHosted
                        ? PrincipalInfo.AnonymousPrincipal
                        : null;

                    //avoid infinite loop
                    //occurs when a page A have another page B in XhtmlString and page B as well have page A in XhtmlString
                    //or if page A have another page B in XhtmlString, page B have another page C in XhtmlString and page C have page A in XhtmlString
                    if (ignoreXhtmlStringContentFragments)
                        return indexText;

                    IEnumerable<ContentFragment> contentFragments = xhtml.GetFragments(principal);

                    foreach (ContentFragment fragment in contentFragments)
                    {
                        if (fragment.ContentLink != null && fragment.ContentLink != ContentReference.EmptyReference)
                        {
                            if (ContentLoader.TryGet(fragment.ContentLink, out IContent fragmentContent) && !Indexer.IsExludedType(fragmentContent))
                            {
                                Type fragmentType = GetContentType(fragmentContent);

                                List<PropertyInfo> indexableProperties = fragmentType.GetIndexableProps(false);
                                indexableProperties.ForEach(property =>
                                {
                                    object indexValue = GetIndexValue(fragmentContent, property, ignoreXhtmlStringContentFragments: true);
                                    indexText += " " + indexValue;
                                });
                            }
                        }
                    }

                    return indexText;
                }

                if (p.PropertyType.IsEnum)
                    return (int)value;

                // Local block
                if (typeof(BlockData).IsAssignableFrom(p.PropertyType))
                {
                    string flattenedValue = null;
                    foreach (PropertyInfo prop in p.PropertyType.GetIndexableProps(false))
                    {
                        flattenedValue += " " + GetIndexValue(value as IContentData, prop);
                    }
                    return flattenedValue;
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
                return null;

            string filePath = null;

            try
            {
                if (content is MediaData mediaData)
                {
                    if (mediaData.BinaryData is FileBlob fileBlob)
                    {
                        filePath = fileBlob.FilePath;
                        if (ElasticSearchSettings.DocumentMaxSize <= 0 || new FileInfo(fileBlob.FilePath).Length <= ElasticSearchSettings.DocumentMaxSize)
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
                && binaryExtensions.Contains(fileExtension.TrimStart('.').ToLowerInvariant());
        }
    }
}
