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
using Epinova.ElasticSearch.Core.Events;
using Epinova.ElasticSearch.Core.Extensions;
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
    public class CoreIndexer : ICoreIndexer
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(CoreIndexer));
        private readonly IElasticSearchSettings _settings;


        public CoreIndexer(IElasticSearchSettings settings)
        {
            _settings = settings;
        }


        public BulkBatchResult Bulk(params BulkOperation[] operations)
        {
            return Bulk(operations, s => { });
        }

        public BulkBatchResult Bulk(IEnumerable<BulkOperation> operations, Action<string> logger)
        {
            var bulkBatchResult = new BulkBatchResult();

            if (operations == null)
                return bulkBatchResult;

            JsonSerializer serializer = GetSerializer();

            var uri = $"{_settings.Host}/_bulk";

            var operationList = operations.ToList();

            operationList.ForEach(operation =>
            {
                if (operation.MetaData.Index == null)
                    operation.MetaData.Index = operation.MetaData.IndexCandidate;

                if (operation.MetaData.Index == null)
                    throw new Exception("Index missing");
            });

            var indexes = operationList
                .Select(o => o.MetaData.Index.ToLower())
                .Distinct()
                .ToList();

            var totalCount = operationList.Count;
            var counter = 0;
            var size = _settings.BulkSize;
            while (operationList.Any())
            {
                var batch = operationList.Take(size).ToList();

                try
                {
                    var sb = new StringBuilder();

                    using (var tw = new StringWriter(sb))
                    {
                        counter += size;
                        var from = counter - size;
                        var to = Math.Min(counter, totalCount);

                        var message = $"Processing batch {from}-{to} of {totalCount}";
                        Logger.Information(message);

                        if (Logger.IsDebugEnabled())
                            message =
                                $"WARNING: Debug logging is enabled, this will have a huge impact on indexing-time for large structures. {message}";

                        logger(message);

                        foreach (var operation in batch)
                        {
                            serializer.Serialize(tw, operation.MetaData);
                            tw.WriteLine();
                            serializer.Serialize(tw, operation.Data);
                            tw.WriteLine();
                        }
                    }

                    var payload = sb.ToString();


                    if (Logger.IsDebugEnabled())
                    {
                        var debugJson = $"[{String.Join(",", payload.Split('\n'))}]";

                        Logger.Debug("JSON PAYLOAD");
                        Logger.Debug(JToken.Parse(debugJson).ToString(Formatting.Indented));
                        logger("JSON PAYLOAD");
                        logger(JToken.Parse(debugJson).ToString(Formatting.Indented));
                    }

                    var results = HttpClientHelper.Post(new Uri(uri), Encoding.UTF8.GetBytes(payload));
                    var stringReader = new StringReader(Encoding.UTF8.GetString(results));

                    var bulkResult = serializer.Deserialize<BulkResult>(new JsonTextReader(stringReader));
                    bulkBatchResult.Batches.Add(bulkResult);
                }
                catch (Exception e)
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
            if (indexName == null)
                indexName = _settings.GetDefaultIndexName(language);

            var uri = $"{_settings.Host}/{indexName}/{type.GetTypeName()}/{id}";

            var exists = HttpClientHelper.Head(new Uri(uri)) == HttpStatusCode.OK;

            if (exists)
            {
                HttpClientHelper.Delete(new Uri(uri));
                Refresh(language);
            }
        }

        public void Update(string id, object objectToUpdate, string indexName, Type type = null)
        {
            if (objectToUpdate == null)
            {
                Logger.Information("objectToUpdate was null, Update aborted");
                return;
            }

            var objectType = type ?? objectToUpdate.GetType();

            // IContent type, prepared by AsIndexItem()
            if (objectType.GetProperty(DefaultFields.Types) != null)
            {
                PerformUpdate(id, objectToUpdate, objectType, indexName);
                return;
            }


            // Custom content, get values via reflection
            dynamic updateItem = new ExpandoObject();
            foreach (var prop in objectType.GetProperties().Where(p => p.GetIndexParameters().Length == 0))
            {
                ((IDictionary<string, object>)updateItem).Add(prop.Name, prop.GetValue(objectToUpdate));
            }
            updateItem.Types = objectType.GetInheritancHierarchyArray();
            PerformUpdate(id, updateItem, objectType, indexName);
        }

        public void ClearBestBets(string indexName, Type indexType, string id)
        {
            var request = new BestBetsRequest(new string[0]);
            var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string json = JsonConvert.SerializeObject(request, jsonSettings);
            byte[] data = Encoding.UTF8.GetBytes(json);
            string uri = $"{_settings.Host}/{indexName}/{indexType.GetTypeName()}/{id}/_update";

            Logger.Information($"Clearing BestBets for id '{id}'");

            HttpClientHelper.Post(new Uri(uri), data);

            if (AfterUpdateBestBet != null)
            {
                Logger.Debug("Firing subscribed event AfterUpdateBestBet");
                AfterUpdateBestBet(new BestBetEventArgs { Index = indexName, Type = indexType, Id = id });
            }
        }

        public void UpdateBestBets(string indexName, Type indexType, string id, string[] terms)
        {
            if (terms == null || terms.Length == 0)
                return;

            var request = new BestBetsRequest(terms);
            var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
            string json = JsonConvert.SerializeObject(request, jsonSettings);
            byte[] data = Encoding.UTF8.GetBytes(json);
            string uri = $"{_settings.Host}/{indexName}/{indexType.GetTypeName()}/{id}/_update";

            Logger.Information("Update BestBets:\n" + JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Post(new Uri(uri), data);

            if (AfterUpdateBestBet != null)
            {
                Logger.Debug("Firing subscribed event AfterUpdateBestBet");
                AfterUpdateBestBet(new BestBetEventArgs { Index = indexName, Type = indexType, Id = id, Terms = terms });
            }
        }

        public void UpdateMapping(Type type, string index)
        {
            UpdateMapping(type, type, index);
        }

        public void UpdateMapping(Type type, Type indexType, string index)
        {
            UpdateMapping(type, indexType, index, null, false);
        }

        public void UpdateMapping(Type type, Type indexType, string index, string language, bool optIn)
        {
            if (type.Name.EndsWith("Proxy"))
                type = type.BaseType;

            language = language ?? _settings.GetLanguage(index);

            // Get indexable properties (string, XhtmlString, [Searchable(true)]) 
            var indexableProperties = type.GetIndexableProps(optIn)
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

            // Custom properties marked for stemming
            indexableProperties.AddRange(Conventions.Indexing.CustomProperties
                .Select(c => new
                {
                    c.Name,
                    c.Type,
                    Analyzable = WellKnownProperties.Analyze.Select(w => w.ToLower()).Contains(c.Name.ToLower())
                }));

            Logger.Information("IndexableProperties for " + type?.Name + ": " + String.Join(", ", indexableProperties.Select(p => p.Name)));

            // Get existing mapping
            IndexMapping mapping = Mapping.GetIndexMapping(indexType, null, index);

            try
            {
                foreach (var prop in indexableProperties)
                {
                    string propName = prop.Name;
                    IndexMappingProperty propertyMapping = mapping.Properties.ContainsKey(prop.Name)
                            ? mapping.Properties[prop.Name]
                            : Language.GetPropertyMapping(language, prop.Type, prop.Analyzable);

                    string mappingType = Mapping.GetMappingTypeAsString(prop.Type);

                    // If mapping with same name exists, use its type. 
                    // Different name/type combos is not allowed.
                    if (mapping.Properties.ContainsKey(prop.Name))
                    {
                        string existingType = mapping.Properties[prop.Name].Type;
                        if (mappingType != existingType)
                        {
                            Logger.Warning($"Conflicting mapping type for property '{propName}' detected. Using already mapped type '{existingType}'");
                        }

                        mappingType = existingType;
                    }

                    if (prop.Analyzable && language != null)
                        propertyMapping.Analyzer = Language.GetLanguageAnalyzer(language);
                    else if (language != null && mappingType == MappingPatterns.StringType)
                        propertyMapping.Analyzer = Language.GetSimpleLanguageAnalyzer(language);

                    // If mapping with different analyzer exists, use its analyzer. 
                    if (mapping.Properties.ContainsKey(prop.Name))
                    {
                        string existingAnalyzer = mapping.Properties[prop.Name].Analyzer;
                        if (propertyMapping.Analyzer != existingAnalyzer)
                        {
                            Logger.Warning($"Conflicting mapping analyzer for property '{propName}' detected. Using already mapped analyzer '{existingAnalyzer}'");
                        }

                        propertyMapping.Analyzer = existingAnalyzer;
                    }

                    if (String.IsNullOrEmpty(propertyMapping.Type) || propertyMapping.Type != mappingType)
                        propertyMapping.Type = mappingType;

                    if (WellKnownProperties.ExcludeFromAll.Contains(propName))
                        propertyMapping.IncludeInAll = false;

                    mapping.AddOrUpdateProperty(propName, propertyMapping);

                    if (Logger.IsDebugEnabled())
                    {
                        Logger.Debug($"Property mapping for '{propName}'");
                        Logger.Debug(propertyMapping.ToString());
                    }
                }

                // Filter out properties with missing type
                mapping.Properties = mapping.Properties
                    .Where(p => p.Value.Type != null)
                    .ToDictionary(d => d.Key, d => d.Value);

                if (!mapping.IsDirty)
                {
                    Logger.Debug("No change in mapping");
                    return;
                }

                var jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                string json = JsonConvert.SerializeObject(mapping, jsonSettings);
                byte[] data = Encoding.UTF8.GetBytes(json);
                string uri = $"{_settings.Host}/{index}/_mapping/{indexType.GetTypeName()}";

                Logger.Information("Update mapping:\n" + JToken.Parse(json).ToString(Formatting.Indented));

                HttpClientHelper.Put(new Uri(uri), data);
            }
            catch (Exception ex)
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
            foreach (string indexName in _settings.Indices.Select(i => _settings.GetCustomIndexName(i, language)))
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

            HttpClientHelper.GetString(new Uri(endpointUri));
        }

        // ReSharper disable once EventNeverSubscribedTo.Global
        public static event OnBeforeUpdateItem BeforeUpdateItem;

        // ReSharper disable once EventNeverSubscribedTo.Global
        public static event OnAfterUpdateBestBet AfterUpdateBestBet;


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
            var indexing = new Indexing(_settings);
            if (!indexing.IndexExists(indexName))
                throw new Exception("Index '" + indexName + "' not found");

            Logger.Information("PerformUpdate: Id=" + id + ", Type=" + objectType.Name + ", Index=" + indexName);

            var endpointUri = $"{_settings.Host}/{indexName}/{objectType.GetTypeName()}/{id}";

            if (BeforeUpdateItem != null)
            {
                Logger.Debug("Firing subscribed event BeforeUpdateItem");
                BeforeUpdateItem(new IndexItemEventArgs { Item = objectToUpdate, Type = objectType });
            }

            string json = Serialization.Serialize(objectToUpdate);
            byte[] data = Encoding.UTF8.GetBytes(json);

            HttpClientHelper.Put(new Uri(endpointUri), data);

            RefreshIndex(indexName);
        }
    }
}
