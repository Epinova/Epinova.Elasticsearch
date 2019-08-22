using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Events;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Bulk;
using Epinova.ElasticSearch.Core.Models.Converters;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Models.Query;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core
{
    [ServiceConfiguration(typeof(ICoreIndexer))]
    internal class CoreIndexer : ICoreIndexer
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CoreIndexer));
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly Mapping _mapping;

        public CoreIndexer(IElasticSearchSettings settings, IHttpClientHelper httpClientHelper)
        {
            _settings = settings;
            _httpClientHelper = httpClientHelper;
            _mapping = new Mapping(settings, httpClientHelper);
        }

        public static event OnBeforeUpdateItem BeforeUpdateItem;
        public static event OnAfterUpdateBestBet AfterUpdateBestBet;

        public BulkBatchResult Bulk(params BulkOperation[] operations)
            => Bulk(operations, _ => { });

        public BulkBatchResult Bulk(IEnumerable<BulkOperation> operations, Action<string> logger)
        {
            var bulkBatchResult = new BulkBatchResult();

            if(operations == null)
            {
                return bulkBatchResult;
            }

            JsonSerializer serializer = GetSerializer();

            var uri = $"{_settings.Host}/_bulk?pipeline={Pipelines.Attachment.Name}";

            var operationList = operations.ToList();

            operationList.ForEach(operation =>
            {
                if(operation.MetaData.Index == null)
                {
                    operation.MetaData.Index = operation.MetaData.IndexCandidate;
                }

                if(operation.MetaData.Index == null)
                {
                    throw new InvalidOperationException("Index missing");
                }
            });

            var indexes = operationList
                .Select(o => o.MetaData.Index.ToLower())
                .Distinct()
                .ToList();

            var totalCount = operationList.Count;
            var counter = 0;
            var size = _settings.BulkSize;
            while(operationList.Count > 0)
            {
                var batch = operationList.Take(size).ToList();

                try
                {
                    var sb = new StringBuilder();

                    using(var tw = new StringWriter(sb))
                    {
                        counter += size;
                        var from = counter - size;
                        var to = Math.Min(counter, totalCount);

                        var message = $"Processing batch {from}-{to} of {totalCount}";
                        Logger.Information(message);

                        if(Logger.IsDebugEnabled())
                        {
                            message = $"WARNING: Debug logging is enabled, this will have a huge impact on indexing-time for large structures. {message}";
                        }

                        logger(message);

                        foreach(BulkOperation operation in batch)
                        {
                            serializer.Serialize(tw, operation.MetaData);
                            tw.WriteLine();
                            serializer.Serialize(tw, operation.Data);
                            tw.WriteLine();
                        }
                    }

                    var payload = sb.ToString();

                    if(Logger.IsDebugEnabled())
                    {
                        var debugJson = $"[{String.Join(",", payload.Split('\n'))}]";

                        Logger.Debug("JSON PAYLOAD");
                        Logger.Debug(JToken.Parse(debugJson).ToString(Formatting.Indented));
                        logger("JSON PAYLOAD");
                        logger(JToken.Parse(debugJson).ToString(Formatting.Indented));
                    }

                    var results = _httpClientHelper.Post(new Uri(uri), Encoding.UTF8.GetBytes(payload));
                    var stringReader = new StringReader(Encoding.UTF8.GetString(results));

                    var bulkResult = serializer.Deserialize<BulkResult>(new JsonTextReader(stringReader));
                    bulkBatchResult.Batches.Add(bulkResult);
                }
                catch(Exception e)
                {
                    Logger.Error("Batch failed", e);
                    logger("Batch failed: " + e.Message);
                }
                finally
                {
                    operationList.RemoveAll(o => batch.Contains(o));
                }
            }

            indexes.ForEach(RefreshIndex);

            return bulkBatchResult;
        }

        public void Delete(string id, string language, Type type, string indexName = null)
        {
            if(indexName == null)
            {
                indexName = _settings.GetDefaultIndexName(language);
            }

            var uri = $"{_settings.Host}/{indexName}/{type.GetTypeName()}/{id}";

            var exists = _httpClientHelper.Head(new Uri(uri)) == HttpStatusCode.OK;

            if(exists)
            {
                _httpClientHelper.Delete(new Uri(uri));
                Refresh(language);
            }
        }

        public void Update(string id, object objectToUpdate, string indexName, Type type = null)
        {
            if(objectToUpdate == null)
            {
                Logger.Information("objectToUpdate was null, Update aborted");
                return;
            }

            Type objectType = type ?? objectToUpdate.GetType();

            // IContent type, prepared by AsIndexItem()
            if(objectType.GetProperty(DefaultFields.Types) != null)
            {
                PerformUpdate(id, objectToUpdate, objectType, indexName);
                return;
            }

            // Custom content, get values via reflection
            dynamic updateItem = new ExpandoObject();
            IEnumerable<PropertyInfo> properties = objectType.GetProperties().Where(p => p.GetIndexParameters().Length == 0);

            foreach(PropertyInfo prop in properties)
            {
                ((IDictionary<string, object>)updateItem).Add(prop.Name, prop.GetValue(objectToUpdate));
            }

            updateItem.Types = objectType.GetInheritancHierarchyArray();
            PerformUpdate(id, updateItem, objectType, indexName);
        }

        public void CreateAnalyzedMappingsIfNeeded(Type type, string language, string indexName = null)
        {
            Logger.Debug("Checking if analyzable mappings needs updating");

            string json = null;
            string oldJson = null;
            IndexMapping mapping = null;

            try
            {
                if(String.IsNullOrWhiteSpace(indexName))
                {
                    indexName = _settings.GetDefaultIndexName(language);
                }

                // Get mappings from server
                mapping = _mapping.GetIndexMapping(typeof(IndexItem), language, indexName);

                // Ignore special mappings
                mapping.Properties.Remove(DefaultFields.AttachmentData);
                mapping.Properties.Remove(DefaultFields.BestBets);
                mapping.Properties.Remove(DefaultFields.DidYouMean);
                mapping.Properties.Remove(DefaultFields.Suggest);
                mapping.Properties.Remove(nameof(IndexItem.attachment));

                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                oldJson = JsonConvert.SerializeObject(mapping, jsonSettings);

                var indexableProperties = GetIndexableProperties(type, false);

                // Get well-known and Stemmed property-names
                List<string> allAnalyzableProperties = indexableProperties.Where(i => i.Analyzable)
                    .Select(i => i.Name)
                    .ToList();

                allAnalyzableProperties.ForEach(p =>
                {
                    IndexMappingProperty propertyMapping = Language.GetPropertyMapping(language, typeof(string), true);

                    mapping.AddOrUpdateProperty(p, propertyMapping);
                });

                if(!mapping.IsDirty)
                {
                    // No change, quit.
                    Logger.Debug("No change");
                    return;
                }

                json = JsonConvert.SerializeObject(mapping, jsonSettings);
                var data = Encoding.UTF8.GetBytes(json);
                var uri = $"{_settings.Host}/{indexName}/_mapping/{typeof(IndexItem).GetTypeName()}";
                if(Server.Info.Version.Major >= 7)
                {
                    uri += "?include_type_name=true";
                }

                Logger.Debug("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

                _httpClientHelper.Put(new Uri(uri), data);
            }
            catch(Exception ex)
            {
                HandleMappingError(type, ex, json, oldJson, mapping);
            }
        }

        public void ClearBestBets(string indexName, Type indexType, string id)
        {
            var request = new BestBetsRequest(new string[0]);
            var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var json = JsonConvert.SerializeObject(request, jsonSettings);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = $"{_settings.Host}/{indexName}/{indexType.GetTypeName()}/{id}/_update";

            Logger.Information($"Clearing BestBets for id '{id}'");

            _httpClientHelper.Post(new Uri(uri), data);

            if(AfterUpdateBestBet != null)
            {
                Logger.Debug("Firing subscribed event AfterUpdateBestBet");
                AfterUpdateBestBet(new BestBetEventArgs { Index = indexName, Type = indexType, Id = id });
            }
        }

        public void UpdateBestBets(string indexName, Type indexType, string id, string[] terms)
        {
            if(terms == null || terms.Length == 0)
            {
                return;
            }

            var request = new BestBetsRequest(terms);
            var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var json = JsonConvert.SerializeObject(request, jsonSettings);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = $"{_settings.Host}/{indexName}/{indexType.GetTypeName()}/{id}/_update";

            Logger.Information("Update BestBets:\n" + JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Post(new Uri(uri), data);

            if(AfterUpdateBestBet != null)
            {
                Logger.Debug("Firing subscribed event AfterUpdateBestBet");
                AfterUpdateBestBet(new BestBetEventArgs { Index = indexName, Type = indexType, Id = id, Terms = terms });
            }

            RefreshIndex(indexName);
        }

        public void UpdateMapping(Type type, string index)
            => UpdateMapping(type, type, index);

        public void UpdateMapping(Type type, Type indexType, string index)
            => UpdateMapping(type, indexType, index, null, false);

        public void UpdateMapping(Type type, Type indexType, string index, string language, bool optIn)
        {
            if(type.Name.EndsWith("Proxy"))
            {
                type = type.BaseType;
            }

            language = language ?? _settings.GetLanguage(index);
            var indexableProperties = GetIndexableProperties(type, optIn);

            Logger.Information("IndexableProperties for " + type?.Name + ": " + String.Join(", ", indexableProperties.Select(p => p.Name)));

            // Get existing mapping
            IndexMapping mapping = _mapping.GetIndexMapping(indexType, null, index);

            // Ignore special mappings
            mapping.Properties.Remove(DefaultFields.AttachmentData);
            mapping.Properties.Remove(DefaultFields.BestBets);
            mapping.Properties.Remove(DefaultFields.DidYouMean);
            mapping.Properties.Remove(DefaultFields.Suggest);

            try
            {
                foreach(var prop in indexableProperties)
                {
                    string propName = prop.Name;
                    IndexMappingProperty propertyMapping = mapping.Properties.ContainsKey(prop.Name)
                            ? mapping.Properties[prop.Name]
                            : Language.GetPropertyMapping(language, prop.Type, prop.Analyzable);

                    string mappingType = Mapping.GetMappingTypeAsString(prop.Type);

                    // If mapping with same name exists, use its type. 
                    // Different name/type combos is not allowed.
                    if(mapping.Properties.ContainsKey(prop.Name))
                    {
                        string existingType = mapping.Properties[prop.Name].Type;
                        if(mappingType != existingType)
                        {
                            Logger.Warning($"Conflicting mapping type '{mappingType}' for property '{propName}' detected. Using already mapped type '{existingType}'");
                        }

                        mappingType = existingType;
                    }

                    if(prop.Analyzable && language != null)
                    {
                        propertyMapping.Analyzer = Language.GetLanguageAnalyzer(language);
                    }
                    else if(!WellKnownProperties.IgnoreAnalyzer.Contains(prop.Name) && language != null && mappingType == nameof(MappingType.Text).ToLower())
                    {
                        propertyMapping.Analyzer = Language.GetSimpleLanguageAnalyzer(language);
                    }

                    if(prop.IncludeInDidYouMean)
                    {
                        if(propertyMapping.CopyTo == null || propertyMapping.CopyTo.Length == 0)
                        {
                            propertyMapping.CopyTo = new[] { DefaultFields.DidYouMean };
                        }
                        else if(!propertyMapping.CopyTo.Contains(DefaultFields.DidYouMean))
                        {
                            propertyMapping.CopyTo = propertyMapping.CopyTo.Concat(new[] { DefaultFields.DidYouMean }).ToArray();
                        }
                    }

                    // If mapping with different analyzer exists, use its analyzer. 
                    if(!WellKnownProperties.IgnoreAnalyzer.Contains(prop.Name) && mapping.Properties.ContainsKey(prop.Name))
                    {
                        string existingAnalyzer = mapping.Properties[prop.Name].Analyzer;
                        if(propertyMapping.Analyzer != existingAnalyzer)
                        {
                            Logger.Warning($"Conflicting mapping analyzer for property '{propName}' detected. Using already mapped analyzer '{existingAnalyzer}'");
                        }

                        propertyMapping.Analyzer = existingAnalyzer;
                    }

                    if(String.IsNullOrEmpty(propertyMapping.Type) || propertyMapping.Type != mappingType)
                    {
                        propertyMapping.Type = mappingType;
                    }

                    mapping.AddOrUpdateProperty(propName, propertyMapping);

                    if(Logger.IsDebugEnabled())
                    {
                        Logger.Debug($"Property mapping for '{propName}'");
                        Logger.Debug(propertyMapping.ToString());
                    }
                }

                // Filter out properties with missing type
                mapping.Properties = mapping.Properties
                    .Where(p => p.Value.Type != null)
                    .ToDictionary(d => d.Key, d => d.Value);

                if(!mapping.IsDirty)
                {
                    Logger.Debug("No change in mapping");
                    return;
                }

                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                var json = JsonConvert.SerializeObject(mapping, jsonSettings);
                var data = Encoding.UTF8.GetBytes(json);
                var uri = $"{_settings.Host}/{index}/_mapping/{indexType.GetTypeName()}";
                if(Server.Info.Version.Major >= 7)
                {
                    uri += "?include_type_name=true";
                }

                Logger.Information("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

                _httpClientHelper.Put(new Uri(uri), data);
            }
            catch(Exception ex)
            {
                Logger.Error($"Failed to update mapping: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Refresh the index
        /// </summary>
        /// <param name="language"></param>
        /// <remarks>https://www.elastic.co/guide/en/elasticsearch/reference/current/indices-refresh.html</remarks>
        public void Refresh(string language)
        {
            foreach(string indexName in _settings.Indices.Select(i => _settings.GetCustomIndexName(i, language)))
            {
                RefreshIndex(indexName);
            }
        }

        /// <summary>
        /// Refresh the index
        /// </summary>
        /// <param name="indexName"></param>
        /// <remarks>https://www.elastic.co/guide/en/elasticsearch/reference/current/indices-refresh.html</remarks>
        public void RefreshIndex(string indexName)
        {
            Logger.Information($"Refreshing index {indexName}");
            var endpointUri = $"{_settings.Host}/{indexName}/_refresh";

            _httpClientHelper.GetString(new Uri(endpointUri));
        }

        private static List<IndexableProperty> GetIndexableProperties(Type type, bool optIn)
        {
            // string, XhtmlString, [Searchable(true)]
            var props = type.GetIndexableProps(optIn)
                .Select(p => new IndexableProperty
                {
                    Name = p.Name,
                    Type = p.PropertyType,
                    IncludeInDidYouMean = ShouldIncludeInDidYouMean(p),
                    Analyzable = IsAnalyzable(p)
                })
                .Where(p => IsValidName(p.Name))
                .ToList();
            var typeList = type.GetInheritancHierarchy();

            // Custom properties marked for stemming
            props.AddRange(Conventions.Indexing.CustomProperties
                .Where(c => typeList.Contains(c.OwnerType))
                .Select(c => new IndexableProperty
                {
                    Name = c.Name,
                    Type = c.Type,
                    Analyzable = WellKnownProperties.Analyze.Select(w => w.ToLower()).Contains(c.Name.ToLower())
                }));

            return props.Distinct().ToList();

            bool IsAnalyzable(PropertyInfo p)
            {
                return ((p.PropertyType == typeof(string) || p.PropertyType == typeof(string[]))
                    && (p.GetCustomAttributes(typeof(StemAttribute)).Any() || WellKnownProperties.Analyze
                            .Select(w => w.ToLower())
                            .Contains(p.Name.ToLower())))
                    || (p.PropertyType == typeof(XhtmlString)
                    && p.GetCustomAttributes(typeof(ExcludeFromSearchAttribute), true).Length == 0);
            }

            bool ShouldIncludeInDidYouMean(PropertyInfo p)
            {
                return (p.PropertyType == typeof(string)
                    || p.PropertyType == typeof(string[])
                    || p.PropertyType == typeof(XhtmlString))
                    && p.GetCustomAttributes(typeof(DidYouMeanSourceAttribute)).Any();
            }

            bool IsValidName(string name)
            {
                return name != nameof(IndexItem.Type)
                    && name != nameof(IndexItem._bestbets)
                    && name != DefaultFields.DidYouMean
                    && name != DefaultFields.Suggest
                    && name != nameof(IndexItem.attachment)
                    && name != nameof(IndexItem._attachmentdata);
            }
        }

        private static JsonSerializer GetSerializer()
        {
            var serializer = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            serializer.Converters.Add(new BulkMetadataConverter());

            return serializer;
        }

        private void PerformUpdate(string id, object objectToUpdate, Type objectType, string indexName)
        {
            var indexing = new Indexing(_settings, _httpClientHelper);
            if(!indexing.IndexExists(indexName))
            {
                throw new InvalidOperationException($"Index '{indexName}' not found");
            }

            Logger.Information("PerformUpdate: Id=" + id + ", Type=" + objectType.Name + ", Index=" + indexName);

            var endpointUri = $"{_settings.Host}/{indexName}/{objectType.GetTypeName()}/{id}";

            if(HasAttachmentData(objectToUpdate))
            {
                endpointUri += $"?pipeline={Pipelines.Attachment.Name}";
            }

            if(BeforeUpdateItem != null)
            {
                Logger.Debug("Firing subscribed event BeforeUpdateItem");
                BeforeUpdateItem(new IndexItemEventArgs { Item = objectToUpdate, Type = objectType });
            }

            var json = Serialization.Serialize(objectToUpdate);
            var data = Encoding.UTF8.GetBytes(json);

            _httpClientHelper.Put(new Uri(endpointUri), data);

            RefreshIndex(indexName);
        }

        private static bool HasAttachmentData(object objectToUpdate)
        {
            return objectToUpdate is IDictionary<string, object> dict
                   && dict.TryGetValue(DefaultFields.AttachmentData, out _);
        }

        private void HandleMappingError(in Type type, in Exception ex, in string json, in string oldJson, in IndexMapping mapping)
        {
            Logger.Error($"Failed to update mappings for content of type '{type.Name}'\n. Properties with the same name but different type, " +
                        "where one of the types is analyzable and the other is not, is often the cause of this error. Ie. 'string MainIntro' vs 'XhtmlString MainIntro'. \n" +
                        "All properties with equal name must be of the same type or ignored from indexing with [Searchable(false)]. \n" +
                        "Enable debug-logging to view further details.", ex);

            if(Logger.IsDebugEnabled())
            {
                Logger.Debug("Old mapping:\n" + JToken.Parse(oldJson ?? String.Empty).ToString(Formatting.Indented));
                Logger.Debug("New mapping:\n" + JToken.Parse(json ?? String.Empty).ToString(Formatting.Indented));

                try
                {
                    IndexMapping oldMappings = JsonConvert.DeserializeObject<IndexMapping>(oldJson);

                    foreach(KeyValuePair<string, IndexMappingProperty> oldMapping in oldMappings.Properties)
                    {
                        if(mapping?.Properties.ContainsKey(oldMapping.Key) == true
                            && !oldMapping.Value.Equals(mapping.Properties[oldMapping.Key]))
                        {
                            Logger.Error("Property '" + oldMapping.Key + "' has different mapping across different types");
                            Logger.Debug("Old: \n" + JsonConvert.SerializeObject(oldMapping.Value));
                            Logger.Debug("New: \n" + JsonConvert.SerializeObject(mapping.Properties[oldMapping.Key]));
                        }
                    }
                }
                catch(Exception e)
                {
                    Logger.Error("Failed to compare mappings", e);
                }
            }
        }
    }
}
