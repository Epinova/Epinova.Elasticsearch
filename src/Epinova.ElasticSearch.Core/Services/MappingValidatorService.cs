using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Core;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.Services
{
    [ServiceConfiguration(ServiceType = typeof(IMappingValidatorService), Lifecycle = ServiceInstanceScope.Transient)]
    public class MappingValidatorService : IMappingValidatorService
    {
        private readonly Mapping _mapping;
        private readonly IServerInfoService _serverInfoService;
        private readonly IElasticSearchSettings _elasticSearchSettings;
        private readonly IHttpClientHelper _httpClientHelper;

        public MappingValidatorService(IServerInfoService serverInfoService, IElasticSearchSettings elasticSearchSettings, IHttpClientHelper httpClientHelper)
        {
            _serverInfoService = serverInfoService;
            _elasticSearchSettings = elasticSearchSettings;
            _httpClientHelper = httpClientHelper;
            _mapping = new Mapping(_serverInfoService, _elasticSearchSettings, _httpClientHelper);
        }

        public List<MappingValidatorType> Validate(string indexName, Type type)
        {
            var errors = new List<MappingValidatorType>();

            IndexMapping currentMappings = _mapping.GetIndexMapping(indexName);

            List<(Type, List<IndexableProperty>)> correctMappings = GetCorrectMappings(indexName, type);

            foreach((Type, List<IndexableProperty>) correctMapping in correctMappings)
            {
                var properties = new List<MappingValidatorProperty>();

                foreach(IndexableProperty indexableProperty in correctMapping.Item2)
                {
                    CoreIndexer.GetPropertyMapping(indexableProperty, _elasticSearchSettings.GetLanguageFromIndexName(indexName), currentMappings, out MappingConflict mappingConflict);
                    if(mappingConflict != MappingConflict.Found)
                    {
                        IEnumerable<string> errorDescriptions = mappingConflict.AsEnumDescriptions();
                        properties.Add(new MappingValidatorProperty(indexableProperty.Name, errors: errorDescriptions));
                    }
                }

                if(properties.Any())
                    errors.Add(new MappingValidatorType(correctMapping.Item1.Name, properties));
            }

            return errors;
        }

        private List<(Type, List<IndexableProperty>)> GetCorrectMappings(string indexName, Type type)
        {
            List<Type> types;

            if(type == typeof(IndexItem))
            {
                Type attributeType = Type.GetType(indexName.IndexOf(Constants.CommerceProviderName, StringComparison.OrdinalIgnoreCase) > 0
                        ? "EPiServer.Commerce.Catalog.DataAnnotations.CatalogContentTypeAttribute, EPiServer.Business.Commerce"
                        : "EPiServer.DataAnnotations.ContentTypeAttribute, EPiServer");
                
                bool isCultureInvariant = indexName.Contains(Constants.InvariantCultureIndexNamePostfix);
                types = GetTypesWithAttribute(attributeType, isCultureInvariant).ToList();
            }
            else
            {
                types = new List<Type> { type };
            }

            return types
                .Where(t => !t.IsExcludedType())
                .Select(t => (t, CoreIndexer.GetIndexableProperties(t, optIn: false)))
                .ToList();
        }

        private IEnumerable<Type> GetTypesWithAttribute(Type attributeType, bool isCultureInvariant)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName).ToList();

            AssemblySettings.IgnoreList.ToList().ForEach(b => assemblies.RemoveAll(a => a.FullName.StartsWith(b, StringComparison.OrdinalIgnoreCase)));
            
            foreach(Assembly assembly in assemblies)
            {
                foreach(Type type in assembly.GetTypes())
                {
                    if(type.CustomAttributes.Any(a => a.AttributeType == attributeType) && ShouldBeIncluded(type))
                        yield return type;
                }
            }

            bool ShouldBeIncluded(Type type)
            {
                bool isAssignableFromMediaData = typeof(MediaData).IsAssignableFrom(type);
                return isCultureInvariant ? isAssignableFromMediaData : !isAssignableFromMediaData;
            }
        }
    }

    public interface IMappingValidatorService
    {
        List<MappingValidatorType> Validate(string indexName, Type type);
    }
}


