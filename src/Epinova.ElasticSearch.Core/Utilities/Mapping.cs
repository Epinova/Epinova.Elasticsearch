﻿using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Enums;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Extensions;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Mapping
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Mapping));
        private static readonly IElasticSearchSettings ElasticSearchSettings =
            ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

        private static readonly Dictionary<MappingType, Type[]> TypeRegister = new Dictionary<MappingType, Type[]>
        {
            {MappingType.Boolean, new[] {typeof (bool), typeof (bool?)}},
            {MappingType.Date, new[] {typeof (DateTime), typeof (DateTime?)}},
            {MappingType.Double, new[] {typeof (double), typeof (double?)}},
            {MappingType.Float, new[] {typeof (float), typeof (float?), typeof (decimal), typeof (decimal?)}},
            {MappingType.Integer, new[] { typeof(Enum), typeof(byte?), typeof(byte), typeof(int), typeof (int?), typeof (uint), typeof (uint?), typeof (short), typeof (short?)}},
            {MappingType.Long, new[] {typeof (long), typeof (long?)}}
        };

        internal static MappingType GetMappingType(Type type)
        {
            if (type == typeof(IntegerRange))
                return MappingType.Integer_Range;

            if (ArrayHelper.IsDictionary(type))
                return MappingType.Object;

            if (type.IsEnum)
                return MappingType.Integer;

            if (ArrayHelper.IsArrayCandidate(type))
                type = type.GetTypeFromTypeCode();

            foreach (var typeEntry in TypeRegister)
            {
                if (typeEntry.Value.Contains(type))
                    return typeEntry.Key;
            }

            return MappingType.Text;
        }

        internal static bool IsNumericType(Type type)
        {
            if (type == null)
                return false;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                case TypeCode.Object:
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsNumericType(Nullable.GetUnderlyingType(type));
                    }
                    return false;
            }
            return false;
        }

        internal static string GetMappingTypeAsString(Type type)
        {
            return GetMappingType(type).ToString().ToLower();
        }

        /// <summary>
        /// Gets all property mappings for the configured index and the supplied type
        /// </summary>
        internal static async Task<IndexMapping> GetIndexMappingAsync(Type type, string language, string index)
        {
            if (String.IsNullOrEmpty(index))
                index = ElasticSearchSettings.GetDefaultIndexName(language);

            index = index.ToLower();

            string typeName = type.GetTypeName();
            string mappingUri = GetMappingUri(index, typeName);
            IndexMapping mappings;

            Logger.Debug($"GetIndexMapping for: {typeName}. Uri: {mappingUri}");

            try
            {
                string mappingJson = await HttpClientHelper.GetStringAsync(new Uri(mappingUri));
                mappings = BuildIndexMapping(mappingJson, index, typeName);
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed to get existing mapping from uri '" + mappingUri + "'", ex);
                mappings = new IndexMapping();
            }

            if (mappings.Properties == null)
                mappings.Properties = new Dictionary<string, IndexMappingProperty>();

            return mappings;
        }

        /// <summary>
        /// Gets all property mappings for the configured index and the supplied type
        /// </summary>
        internal static IndexMapping GetIndexMapping(Type type, string language, string index)
        {
            if (String.IsNullOrEmpty(index))
                index = ElasticSearchSettings.GetDefaultIndexName(language);

            string typeName = type.GetTypeName();
            string mappingUri = GetMappingUri(index, typeName);
            IndexMapping mappings;

            Logger.Debug($"GetIndexMapping for: {typeName}. Uri: {mappingUri}");

            try
            {
                mappings = BuildIndexMapping(HttpClientHelper.GetString(new Uri(mappingUri)), index, typeName);
            }
            catch (Exception ex)
            {
                Logger.Debug("Failed to get existing mapping from uri '" + mappingUri + "'", ex);
                mappings = new IndexMapping();
            }

            if (mappings.Properties == null)
                mappings.Properties = new Dictionary<string, IndexMappingProperty>();

            return mappings;
        }

        private static IndexMapping BuildIndexMapping(string mappingJson, string index, string typeName)
        {
            mappingJson = mappingJson.Replace(" ", "");
            mappingJson = mappingJson.Replace("\"" + index + "\":", "");
            mappingJson = mappingJson.Replace("\"" + typeName + "\":", "");
            mappingJson = mappingJson.Replace("\"mappings\":", "");

            Regex regex = new Regex(Regex.Escape("}}}"), RegexOptions.RightToLeft);
            mappingJson = regex.Replace(mappingJson, "", 1);
            regex = new Regex(Regex.Escape("{{{"));
            mappingJson = regex.Replace(mappingJson, "", 1);

            return JsonConvert.DeserializeObject<IndexMapping>(mappingJson);
        }

        private static string GetMappingUri(string index, string typeName)
        {
            return $"{ElasticSearchSettings.Host}/{index}/{typeName}/_mapping";
        }
    }
}
