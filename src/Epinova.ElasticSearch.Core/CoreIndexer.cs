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
using Epinova.ElasticSearch.Core.Models.Admin;
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
    [ServiceConfiguration(typeof(ICoreIndexer), Lifecycle = ServiceInstanceScope.Singleton)]
    internal class CoreIndexer : ICoreIndexer
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(CoreIndexer));
        private readonly IServerInfoService _serverInfoService;
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly Mapping _mapping;
        private readonly ServerInfo _serverInfo;

        public CoreIndexer(
            IServerInfoService serverInfoService,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper)
        {
            _serverInfoService = serverInfoService;
            _settings = settings;
            _httpClientHelper = httpClientHelper;
            _mapping = new Mapping(serverInfoService, settings, httpClientHelper);
            _serverInfo = serverInfoService.GetInfo();
        }

        public event OnBeforeUpdateItem BeforeUpdateItem;
        public event OnAfterUpdateBestBet AfterUpdateBestBet;

        public BulkBatchResult Bulk(params BulkOperation[] operations)
            => Bulk(operations, _ => { });

        public BulkBatchResult Bulk(IEnumerable<BulkOperation> operations, Action<string> logger)
        {
            var bulkBatchResult = new BulkBatchResult();

            if(operations == null)
            {
                return bulkBatchResult;
            }

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
                    var ms = new MemoryStream();

                    using(var sw = new StreamWriter(ms, Encoding.UTF8, 1024, true))
                    {
                        counter += size;
                        var from = counter - size;
                        var to = Math.Min(counter, totalCount);

                        if(totalCount > 1)
                        {
                            _logger.Information($"Processing batch {from}-{to} of {totalCount}");
                        }

                        if(_logger.IsDebugEnabled())
                        {
                            logger("WARNING: Debug logging is enabled, this will have a huge impact on indexing-time for large structures.");
                        }

                        foreach(BulkOperation operation in batch)
                        {
                            using(var jsonTextWriter = new JsonTextWriter(sw))
                            {
                                var serializer = GetSerializer();

                                serializer.Serialize(jsonTextWriter, operation.MetaData);
                                jsonTextWriter.WriteWhitespace("\n");

                                try
                                {
                                    serializer.Serialize(jsonTextWriter, operation.Data);
                                }
                                catch(OutOfMemoryException)
                                {
                                    _logger.Warning($"Failed to process operation {operation.MetaData.Id}. Too much data.");
                                }

                                jsonTextWriter.WriteWhitespace("\n");
                                jsonTextWriter.Flush();
                            }
                        }
                    }

                    if(ms.CanSeek)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                    }

                    var results = _httpClientHelper.Post(new Uri(uri), ms);
                    var stringReader = new StringReader(Encoding.UTF8.GetString(results));

                    var bulkResult = GetSerializer().Deserialize<BulkResult>(new JsonTextReader(stringReader));
                    bulkBatchResult.Batches.Add(bulkResult);
                }
                catch(Exception e)
                {
                    _logger.Error("Batch failed", e);
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
                _logger.Information("objectToUpdate was null, Update aborted");
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
            _logger.Debug("Checking if analyzable mappings needs updating");

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
                    _logger.Debug("No change");
                    return;
                }

                json = JsonConvert.SerializeObject(mapping, jsonSettings);
                var data = Encoding.UTF8.GetBytes(json);
                var uri = $"{_settings.Host}/{indexName}/_mapping/{typeof(IndexItem).GetTypeName()}";
                if(_serverInfo.Version >= Constants.IncludeTypeNameAddedVersion)
                {
                    uri += "?include_type_name=true";
                }

                _logger.Debug("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

                _httpClientHelper.Put(new Uri(uri), data);
            }
            catch(Exception ex)
            {
                var message = HandleMappingError(type, ex, json, oldJson, mapping) + " \n\n" + ex.Message;
                throw new Exception(message);
            }
        }

        public void ClearBestBets(string indexName, Type indexType, string id)
        {
            var request = new BestBetsRequest(Array.Empty<string>());
            var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var json = JsonConvert.SerializeObject(request, jsonSettings);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = $"{_settings.Host}/{indexName}/{indexType.GetTypeName()}/{id}/_update";

            _logger.Information($"Clearing BestBets for id '{id}'");

            _httpClientHelper.Post(new Uri(uri), data);

            if(AfterUpdateBestBet != null)
            {
                _logger.Debug("Firing subscribed event AfterUpdateBestBet");
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

            _logger.Information("Update BestBets:\n" + JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Post(new Uri(uri), data);

            if(AfterUpdateBestBet != null)
            {
                _logger.Debug("Firing subscribed event AfterUpdateBestBet");
                AfterUpdateBestBet(new BestBetEventArgs { Index = indexName, Type = indexType, Id = id, Terms = terms });
            }

            RefreshIndex(indexName);
        }

        public void UpdateMapping(Type type, Type indexType, string index) => UpdateMapping(type, indexType, index, null, false);

        public void UpdateMapping(Type type, Type indexType, string index, string language, bool optIn)
        {
            if(type.Name.EndsWith("Proxy"))
            {
                type = type.BaseType;
            }

            language = language ?? _settings.GetLanguage(index);
            var indexableProperties = GetIndexableProperties(type, optIn);
            var typeName = type?.Name;

            _logger.Information("IndexableProperties for " + typeName + ": " + String.Join(", ", indexableProperties.Select(p => p.Name)));

            // Get existing mapping
            IndexMapping mapping = _mapping.GetIndexMapping(indexType, null, index);

            // Ignore special mappings
            mapping.Properties.Remove(DefaultFields.AttachmentData);
            mapping.Properties.Remove(DefaultFields.BestBets);
            mapping.Properties.Remove(DefaultFields.DidYouMean);
            mapping.Properties.Remove(DefaultFields.Suggest);

            foreach(IndexableProperty prop in indexableProperties)
            {
                string propName = prop.Name;
                IndexMappingProperty propertyMapping = GetPropertyMapping(prop, language, mapping, out _);

                mapping.AddOrUpdateProperty(propName, propertyMapping);

                if(_logger.IsDebugEnabled())
                {
                    _logger.Debug($"Property mapping for '{propName}'");
                    _logger.Debug(propertyMapping.ToString());
                }
            }

            // Filter out properties with missing type
            mapping.Properties = mapping.Properties
                .Where(p => p.Value.Type != null)
                .ToDictionary(d => d.Key, d => d.Value);

            if(!mapping.IsDirty)
            {
                _logger.Debug("No change in mapping");
                return;
            }

            var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            var json = JsonConvert.SerializeObject(mapping, jsonSettings);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = $"{_settings.Host}/{index}/_mapping/{indexType.GetTypeName()}";
            if(_serverInfo.Version >= Constants.IncludeTypeNameAddedVersion)
            {
                uri += "?include_type_name=true";
            }

            _logger.Information("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Put(new Uri(uri), data);
        }

        internal static IndexMappingProperty GetPropertyMapping(IndexableProperty prop, string language, IndexMapping existingMapping, out MappingConflict mappingConflict)
        {
            var propName = prop.Name;
            IndexMappingProperty propertyMapping;

            if(existingMapping.Properties.ContainsKey(propName))
            {
                propertyMapping = existingMapping.Properties[propName];
                mappingConflict = MappingConflict.Found;
            }
            else
            {
                propertyMapping = Language.GetPropertyMapping(language, prop.Type, prop.Analyzable);
                mappingConflict = MappingConflict.Missing;
            }

            string mappingType = Mapping.GetMappingTypeAsString(prop.Type);

            // If mapping with same name exists, use its type. 
            // Different name/type combos is not allowed.
            if(existingMapping.Properties.ContainsKey(propName))
            {
                string existingType = existingMapping.Properties[propName].Type;
                if(mappingType != existingType)
                {
                    _logger.Warning($"Conflicting mapping type '{mappingType}' for property '{propName}' detected. Using already mapped type '{existingType}'");
                    mappingConflict = mappingConflict | MappingConflict.Mapping;
                }

                mappingType = existingType;
            }

            var analyzerFull = Language.GetLanguageAnalyzer(language);
            var analyzerSimple = Language.GetSimpleLanguageAnalyzer(language);

            if(prop.Analyzable && language != null)
            {
                propertyMapping.Analyzer = analyzerFull;
            }
            else if(!WellKnownProperties.IgnoreAnalyzer.Contains(propName)
                && language != null
                && mappingType == nameof(MappingType.Text).ToLower()
                && propertyMapping.Analyzer != analyzerFull)
            {
                propertyMapping.Analyzer = analyzerSimple;
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
            if(!WellKnownProperties.IgnoreAnalyzer.Contains(propName) && existingMapping.Properties.ContainsKey(propName))
            {
                string existingAnalyzer = existingMapping.Properties[propName].Analyzer;
                if(propertyMapping.Analyzer != existingAnalyzer)
                {
                    _logger.Warning($"Conflicting mapping analyzer for property '{propName}' detected. Using already mapped analyzer '{existingAnalyzer}'");
                    mappingConflict = mappingConflict | MappingConflict.Analyzer;
                }

                propertyMapping.Analyzer = existingAnalyzer;
            }

            if(String.IsNullOrEmpty(propertyMapping.Type) || propertyMapping.Type != mappingType)
            {
                propertyMapping.Type = mappingType;
            }

            return propertyMapping;
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
            _logger.Debug($"Refreshing index {indexName}");
            var endpointUri = $"{_settings.Host}/{indexName}/_refresh";

            _httpClientHelper.GetString(new Uri(endpointUri));
        }

        public static List<IndexableProperty> GetIndexableProperties(Type type, bool optIn)
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

            static bool IsAnalyzable(PropertyInfo p)
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

            static bool IsValidName(string name)
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
            var indexing = new Indexing(_serverInfoService, _settings, _httpClientHelper);
            if(!indexing.IndexExists(indexName))
            {
                _logger.Error($"Index '{indexName}' not found");
                return;
            }

            _logger.Information("PerformUpdate: Id=" + id + ", Type=" + objectType.Name + ", Index=" + indexName);

            var endpointUri = $"{_settings.Host}/{indexName}/{objectType.GetTypeName()}/{id}";

            if(HasAttachmentData(objectToUpdate))
            {
                endpointUri += $"?pipeline={Pipelines.Attachment.Name}";
            }

            if(BeforeUpdateItem != null)
            {
                _logger.Debug("Firing subscribed event BeforeUpdateItem");
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

        private string HandleMappingError(Type type, Exception ex, string json, string oldJson, IndexMapping mapping)
        {
            var message = $"Failed to update mappings for content of type '{type.Name}.'\n" +
                        "Properties with the same name and different type or stemming is often the cause of this error.\n" +
                        "Ie. 'string MainIntro' in one type vs 'XhtmlString MainIntro' in another type, " +
                        "or 'string MainIntro' in one type vs '[Stem]string MainIntro in another type.'\n" +
                        "All properties with equal name must be of the same type and have the same analyzer, " +
                        "or be ignored from indexing with [Searchable(false)].";

            _logger.Error(message, ex);

            if(_logger.IsDebugEnabled())
            {
                _logger.Debug("Old mapping:\n" + JToken.Parse(oldJson ?? String.Empty).ToString(Formatting.Indented));
                _logger.Debug("New mapping:\n" + JToken.Parse(json ?? String.Empty).ToString(Formatting.Indented));

                try
                {
                    IndexMapping oldMappings = JsonConvert.DeserializeObject<IndexMapping>(oldJson);

                    foreach(KeyValuePair<string, IndexMappingProperty> oldMapping in oldMappings.Properties)
                    {
                        if(mapping?.Properties.ContainsKey(oldMapping.Key) == true
                            && !oldMapping.Value.Equals(mapping.Properties[oldMapping.Key]))
                        {
                            _logger.Error("Property '" + oldMapping.Key + "' has different mapping across different types");
                            _logger.Debug("Old: \n" + JsonConvert.SerializeObject(oldMapping.Value));
                            _logger.Debug("New: \n" + JsonConvert.SerializeObject(mapping.Properties[oldMapping.Key]));
                        }
                    }
                }
                catch(Exception e)
                {
                    _logger.Error("Failed to compare mappings", e);
                }
            }

            return message;
        }
    }
}
