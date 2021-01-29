using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
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

        public List<MappingValidatorType> Validate(IndexInformation index)
        {
            var errors = new List<MappingValidatorType>();

            IndexMapping currentMappings = GetCurrentIndexMappings(index);

            List<(Type, List<IndexableProperty>)> correctMappings = GetCorrectMappings(index);

            foreach((Type, List<IndexableProperty>) correctMapping in correctMappings)
            {
                var properties = new List<MappingValidatorProperty>();

                foreach(IndexableProperty indexableProperty in correctMapping.Item2)
                {
                    CoreIndexer.GetPropertyMapping(indexableProperty, "no", currentMappings, out MappingConflict mappingConflict);
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

        private List<(Type, List<IndexableProperty>)> GetCorrectMappings(IndexInformation index)
        {
            Type attributeType;

            if(index.Index.IndexOf(Constants.CommerceProviderName, StringComparison.OrdinalIgnoreCase) > 0)
                attributeType = Type.GetType("EPiServer.Commerce.Catalog.DataAnnotations.CatalogContentTypeAttribute, EPiServer.Business.Commerce");
            else
                attributeType = Type.GetType("EPiServer.DataAnnotations.ContentTypeAttribute, EPiServer");
            //handle other indextypes?

            return GetTypesWithAttribute(attributeType)
                .Where(t => !t.IsExcludedType())
                .Select(type => (type, CoreIndexer.GetIndexableProperties(type, optIn: false)))
                .ToList();
        }

        private IEnumerable<Type> GetTypesWithAttribute(Type attributeType)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName).ToList();

            AssemblySettings.IgnoreList.ToList().ForEach(b => assemblies.RemoveAll(a => a.FullName.StartsWith(b, StringComparison.OrdinalIgnoreCase)));
            
            foreach(Assembly assembly in assemblies)
            {
                foreach(Type type in assembly.GetTypes())
                {
                    if(type.CustomAttributes.Any(a => a.AttributeType == attributeType))
                        yield return type;
                }
            }
        }

        private IndexMapping GetCurrentIndexMappings(IndexInformation indexConfig)
        {
            Type type = String.IsNullOrEmpty(indexConfig.Type) || indexConfig.Type == "[default]"
                ? typeof(IndexItem)
                : Type.GetType(indexConfig.Type);
            return _mapping.GetIndexMapping(type, null, indexConfig.Index);
        }
    }

    public interface IMappingValidatorService
    {
        List<MappingValidatorType> Validate(IndexInformation index);
    }
}


