using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Logging;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal class Mapping
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(Mapping));
     
        private static readonly Dictionary<MappingType, Type[]> TypeRegister = new Dictionary<MappingType, Type[]>
        {
            {MappingType.Boolean, new[] {typeof (bool), typeof (bool?)}},
            {MappingType.Date, new[] {typeof (DateTime), typeof (DateTime?)}},
            {MappingType.Double, new[] {typeof (double), typeof (double?)}},
            {MappingType.Float, new[] {typeof (float), typeof (float?), typeof (decimal), typeof (decimal?)}},
            {MappingType.Integer, new[] { typeof(Enum), typeof(byte?), typeof(byte), typeof(int), typeof (int?), typeof (uint), typeof (uint?), typeof (short), typeof (short?)}},
            {MappingType.Long, new[] {typeof (long), typeof (long?)}}
        };
        
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IElasticSearchSettings _settings;
        private readonly ServerInfo _serverInfo;


        public Mapping(IServerInfoService serverInfoService, IElasticSearchSettings settings, IHttpClientHelper httpClientHelper)
        {
            _serverInfo = serverInfoService.GetInfo();
            _settings = settings;
            _httpClientHelper = httpClientHelper;
        }

        internal static MappingType GetMappingType(Type type)
        {
            if(type == typeof(IntegerRange))
            {
                return MappingType.Integer_Range;
            }

            if(type == typeof(GeoPoint))
            {
                return MappingType.Geo_Point;
            }

            if(ArrayHelper.IsDictionary(type))
            {
                return MappingType.Object;
            }

            if(type.IsEnum)
            {
                return MappingType.Integer;
            }

            if(ArrayHelper.IsArrayCandidate(type))
            {
                type = type.GetTypeFromTypeCode();
            }

            foreach(var typeEntry in TypeRegister)
            {
                if(typeEntry.Value.Contains(type))
                {
                    return typeEntry.Key;
                }
            }

            return MappingType.Text;
        }

        internal static bool IsNumericType(Type type)
        {
            if(type == null)
            {
                return false;
            }

            switch(Type.GetTypeCode(type))
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
                    if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return IsNumericType(Nullable.GetUnderlyingType(type));
                    }
                    return false;
            }
            return false;
        }

        internal static string GetMappingTypeAsString(Type type)
            => GetMappingType(type).ToString().ToLower();

        /// <summary>
        /// Gets all property mappings for the configured index and the supplied type
        /// </summary>
        internal IndexMapping GetIndexMapping(Type type, string index)
        {
            string typeName = type.GetTypeName();
            Uri mappingUri = GetMappingUri(index, typeName);
            IndexMapping mappings;

            _logger.Debug($"GetIndexMapping for: {typeName}. Uri: {mappingUri}");

            try
            {
                string mappingJson = _httpClientHelper.GetString(mappingUri);
                bool isSingleType = _serverInfo.Version >= Constants.SingleTypeMappingVersion;
                mappings = BuildIndexMapping(isSingleType, mappingJson, index, typeName);
            }
            catch(Exception ex)
            {
                _logger.Debug("Failed to get existing mapping from uri '" + mappingUri + "'", ex);
                mappings = new IndexMapping();
            }

            if(mappings.Properties == null)
                mappings.Properties = new Dictionary<string, IndexMappingProperty>();

            return mappings;
        }

        private static IndexMapping BuildIndexMapping(bool isSingleType, string mappingJson, string index, string typeName)
        {
            if(isSingleType) //should work for Elastic 7 as well
            {
                mappingJson = mappingJson.Replace(index, "indexnamereplacement");
                return JsonConvert.DeserializeObject<IndexSettings>(mappingJson)?.IndexNameReplacement?.Mappings ?? new IndexMapping();
            }

            return GetIndexMappingNonSingleType(mappingJson, index, typeName);
        }

        private static IndexMapping GetIndexMappingNonSingleType(string mappingJson, string index, string typeName)
        {
            mappingJson = mappingJson.Replace(" ", "");
            mappingJson = mappingJson.Replace("\"" + index + "\":", "");
            mappingJson = mappingJson.Replace("\"" + typeName + "\":", "");
            mappingJson = mappingJson.Replace("\"mappings\":", "");

            var regex = new Regex(Regex.Escape("}}}"), RegexOptions.RightToLeft);
            mappingJson = regex.Replace(mappingJson, "", 1);
            regex = new Regex(Regex.Escape("{{{"));
            mappingJson = regex.Replace(mappingJson, "", 1);

            if(string.IsNullOrWhiteSpace(mappingJson))
                return new IndexMapping();

            return JsonConvert.DeserializeObject<IndexMapping>(mappingJson);
        }

        internal Uri GetMappingUri(string indexName, string type = null)
        {
            type = type != null && _serverInfo.Version < Constants.SingleTypeMappingVersion ? String.Concat("/", type) : null;
            
            var uri = $"{_settings.Host}/{indexName}{type}/_mapping";

            if(_serverInfo.Version >= Constants.IncludeTypeNameAddedVersion && _serverInfo.Version < Constants.SingleTypeMappingVersion)
                uri += "?include_type_name=true";

            return new Uri(uri);
        }
    }
}
