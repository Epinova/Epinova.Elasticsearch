using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Models.Mapping;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.DataAbstraction;
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
        private readonly ILanguageBranchRepository _languageBranchRepository;

        public MappingValidatorService(IServerInfoService serverInfoService, IElasticSearchSettings elasticSearchSettings, IHttpClientHelper httpClientHelper, ILanguageBranchRepository languageBranchRepository)
        {
            _serverInfoService = serverInfoService;
            _elasticSearchSettings = elasticSearchSettings;
            _httpClientHelper = httpClientHelper;
            _languageBranchRepository = languageBranchRepository;
            _mapping = new Mapping(_serverInfoService, _elasticSearchSettings, _httpClientHelper);
        }

        //private static ElasticSearchSection Configuration => ElasticSearchSection.GetConfiguration();

        public List<MappingValidatorType> Validate(IndexInformation index)
        {
            var errors = new List<MappingValidatorType>();

            IndexMapping currentMappings = GetCurrentIndexMappings(index);

            //foreach(KeyValuePair<string, string> language in GetLanguages)
            //{
            //    ;

            //    if(currentMappings == null)
            //    {
            //        log.Add($"{index.Index} not created");
            //        return false;
            //    }
            //}

            List<(Type, List<IndexableProperty>)> correctMappings = GetCorrectMappings(index);

            foreach((Type, List<IndexableProperty>) correctMapping in correctMappings)
            {
                var properties = new List<MappingValidatorProperty>();

                foreach(IndexableProperty indexableProperty in correctMapping.Item2)
                {
                    IndexMappingProperty indexMappingProperty = CoreIndexer.GetPropertyMapping(indexableProperty, "no", currentMappings, out MappingConflict mappingConflict);
                    var log = new List<string>();
                    //if(mappingConflict.HasFlag(MappingConflict.Found))
                    //    log.Add("Found existing mapping");
                    if(mappingConflict.HasFlag(MappingConflict.Missing))
                        log.Add("Missing mapping");
                    if(mappingConflict.HasFlag(MappingConflict.Mapping))
                        log.Add("Mapping conflict");
                    if(mappingConflict.HasFlag(MappingConflict.Analyzer))
                        log.Add("Analyzer conflict");

                    if(log.Any())
                        properties.Add(new MappingValidatorProperty(indexableProperty.Name, log.ToArray()));
                }

                if(properties.Any())
                    errors.Add(new MappingValidatorType(correctMapping.Item1.Name, properties));
            }

            return errors;
        }

        private List<(Type, List<IndexableProperty>)> GetCorrectMappings(IndexInformation index)
        {
            //TODO Look @ IndexEPiServerContent
            //var content =  _contentLoader.GetDescendents(ContentReference.RootPage).ToList();
            Type attributeType;

            if(index.Index.IndexOf(Constants.CommerceProviderName, StringComparison.OrdinalIgnoreCase) > 0)
            {
                attributeType = Type.GetType("EPiServer.Commerce.Catalog.DataAnnotations.CatalogContentTypeAttribute, EPiServer.Business.Commerce");
            }
            else
            {
                attributeType = Type.GetType("EPiServer.DataAnnotations.ContentTypeAttribute, EPiServer");
            }
            //other indextypes?

            IEnumerable<Type> typesWithAttribute = GetTypesWithAttribute(attributeType);

            var list = new List<(Type, List<IndexableProperty>)>();
            foreach(Type type in typesWithAttribute)
            {
                if(!type.IsExcludedType())
                    list.Add((type, CoreIndexer.GetIndexableProperties(type, false)));
            }

            return list;
        }

        private IEnumerable<Type> GetTypesWithAttribute(Type attributeType)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName).ToList();

            AssemblySettings.Blacklist.ToList().ForEach(b => assemblies.RemoveAll(a => a.FullName.StartsWith(b, StringComparison.OrdinalIgnoreCase)));
            
            foreach(Assembly assembly in assemblies)
            {
                IEnumerable<Type> types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch(Exception ex)
                {
                    var test = "";
                    continue;
                }

                foreach(Type type in types)
                {
                    if(type.CustomAttributes.Any(a => a.AttributeType == attributeType)) // != null && !string.IsNullOrWhiteSpace(a.AttributeType.FullName) && a.AttributeType.FullName.Equals(attributeType.FullName)))
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

        private Dictionary<string, string> GetLanguages => _languageBranchRepository.ListEnabled()
            .ToDictionary(x => x.LanguageID, x => x.Name);
    }

    public interface IMappingValidatorService
    {
        List<MappingValidatorType> Validate(IndexInformation index);
    }
}


