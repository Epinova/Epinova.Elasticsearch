using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Utilities
{
    /// <summary>
    /// Handles raw-mappings on fields used by aggregations. 
    /// Maintains a register with fields which should have a _raw equivalent with a custom analyzer. 
    /// <see cref="Analyzers.Raw"/>
    /// </summary>
    internal static class RawMapper
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(RawMapper));
        private static readonly ConcurrentDictionary<string, MappingType> Fields = new ConcurrentDictionary<string, MappingType>();
        private static readonly ConcurrentDictionary<string, bool> Initialized = new ConcurrentDictionary<string, bool>();
        private static readonly IElasticSearchSettings ElasticSearchSettings =
            ServiceLocator.Current.GetInstance<IElasticSearchSettings>();


        internal static void Register(string fieldName, MappingType mappingType, string language)
        {
            if (!Fields.ContainsKey(fieldName))
            {
                Logger.Debug($"Registering field: {fieldName} ({mappingType})");
                Fields.AddOrUpdate(fieldName, mappingType, (key, oldValue) => oldValue);
                Initialized.AddOrUpdate(language, false, (key, oldValue) => false);
            }
        }


        internal static bool TryRegister(Dictionary<string, MappingType> facetFieldNames, string language)
        {
            bool result = false;
            facetFieldNames.ToList().ForEach(x =>
            {
                if (!Fields.ContainsKey(x.Key))
                    result = true;

                Register(x.Key, x.Value, language);
            });

            return result;
        }


        internal static void Register(Dictionary<string, MappingType> facetFieldNames, string language)
        {
            facetFieldNames.ToList().ForEach(x => Register(x.Key, x.Value, language));
        }


        //TODO: Check if we can ommit type-property on non-strings
        internal static async Task UpdateMappingsAsync(Type type, string language, bool forceMappingUpdate, string indexName, CancellationToken cancellationToken)
        {
            if (Server.Info.Version.Major >= 5)
            {
                Logger.Debug("Skipping update of mappings for v5");
                return;
            }

            if (IsInitialized(language, forceMappingUpdate))
                return;

            if (String.IsNullOrEmpty(indexName))
                indexName = ElasticSearchSettings.GetDefaultIndexName(language);

            Initialized.AddOrUpdate(language, true, (key, oldValue) => true);
            IndexMapping existingMappings = await Mapping.GetIndexMappingAsync(type, language, indexName);
            LogUpdateInfo(existingMappings);

            Fields.ToList().ForEach(async field =>
            {
                byte[] data = GetMappingData(existingMappings, type, field);
                await HttpClientHelper.PutAsync(GetMappingUri(type, indexName), data, cancellationToken);
            });
        }


        //TODO: Check if we can ommit type-property on non-strings
        internal static void UpdateMappings(Type type, string language, bool forceMappingUpdate, string indexName)
        {
            if (Server.Info.Version.Major >= 5)
            {
                Logger.Debug("Skipping update of mappings for v5");
                return;
            }

            if (IsInitialized(language, forceMappingUpdate))
                return;

            if (String.IsNullOrEmpty(indexName))
                indexName = ElasticSearchSettings.GetDefaultIndexName(language);

            Initialized.AddOrUpdate(language, true, (key, oldValue) => true);
            IndexMapping existingMappings = Mapping.GetIndexMapping(type, language, indexName);
            LogUpdateInfo(existingMappings);

            Fields.ToList().ForEach(field =>
            {
                byte[] data = GetMappingData(existingMappings, type, field);
                HttpClientHelper.Put(GetMappingUri(type, indexName), data);
            });
        }

        private static Uri GetMappingUri(Type type, string indexName)
        {
            string mappingUri = $"{ElasticSearchSettings.Host}/{indexName}/{type.GetTypeName()}/_mapping";

            return new Uri(mappingUri);
        }

        //TODO: Refactor horrendous string format
        private static byte[] GetMappingData(IndexMapping existingMappings, Type type, KeyValuePair<string, MappingType> field)
        {
            string analyzer = String.Empty;
            if (existingMappings.Properties.ContainsKey(field.Key) && existingMappings.Properties[field.Key].Analyzer != null)
                analyzer = "\"analyzer\" : \"" + existingMappings.Properties[field.Key].Analyzer + "\",";

            string copyToJson = "[ \"FIELDNAME_TOKEN" + Models.Constants.RawSuffix + "\"";
            string[] copyTo = existingMappings.Properties.ContainsKey(field.Key)
                ? existingMappings.Properties[field.Key].CopyTo
                : null;
            if (copyTo != null && copyTo.Length > 0)
            {
                copyToJson += ", \"" + String.Join("\", \"", copyTo) + "\"";
            }
            copyToJson += "]";

            string jsonTemplate = "{"
                                  + "  \"" + type.GetTypeName() + "\" : {"
                                  + "    \"properties\" : {"
                                  + "      \"FIELDNAME_TOKEN\" : {"
                                  + "        \"type\" : \"FIELDTYPE_TOKEN\","
                                  + "        " + analyzer
                                  + "        \"copy_to\" : " + copyToJson
                                  + "      }, "
                                  + "      \"FIELDNAME_TOKEN" + Models.Constants.RawSuffix + "\" : {"
                                  + "        \"type\" : \"FIELDTYPE_TOKEN\", "
                                  + "        \"analyzer\" : \"raw\" "
                                  + "      }"
                                  + "    }"
                                  + "  }"
                                  + "}";

            string query = jsonTemplate
                .Replace("FIELDNAME_TOKEN", field.Key)
                .Replace("FIELDTYPE_TOKEN", field.Value.ToString().ToLower());

            Logger.Debug(JToken.Parse(query).ToString(Formatting.Indented));

            return Encoding.UTF8.GetBytes(query);
        }

        private static void LogUpdateInfo(IndexMapping existingMappings)
        {
            if (Logger.IsDebugEnabled())
            {
                Logger.Debug("Existing mappings:");
                foreach (var mapping in existingMappings.Properties)
                    Logger.Debug($"{mapping.Key}: {mapping.Value}");
            }

            Logger.Warning("Updated raw mappings. Content must be re-index to allow facets on newly added fields.");
        }

        private static bool IsInitialized(string language, bool forceMappingUpdate)
        {
            return Initialized.ContainsKey(language)
                   && Initialized.TryGetValue(language, out bool isInitialized)
                   && isInitialized
                   && !forceMappingUpdate;
        }
    }
}
