using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.EPiServer.Services
{
    [ServiceConfiguration(ServiceType = typeof(IInspectorRepository), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class InspectorRepository : IInspectorRepository
    {
        private readonly IElasticSearchSettings _elasticSearchSettings;
        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly Mapping _mapping;
        private readonly ServerInfo _serverInfo;

        internal InspectorRepository()
        {
            _elasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        }

        public InspectorRepository(
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper,
            IContentTypeRepository contentTypeRepository)
        {
            _elasticSearchSettings = settings;
            _contentTypeRepository = contentTypeRepository;
            _httpClientHelper = httpClientHelper;
            _mapping = new Mapping(serverInfoService, settings, httpClientHelper);
            _serverInfo = serverInfoService.GetInfo();
        }

        public List<InspectItem> Search(string searchText, bool analyzed, string language, string indexName, int size, string type = null, string selectedIndex = null)
        {
            if(String.IsNullOrWhiteSpace(searchText) && String.IsNullOrWhiteSpace(type))
            {
                return new List<InspectItem>();
            }

            string uri = $"{_elasticSearchSettings.Host}/{indexName}/_search";
            string response = null;

            if(analyzed)
            {
                var matchQuery = new
                {
                    size,
                    query = new
                    {
                        multi_match = new
                        {
                            query = searchText,
                            lenient = true,
                            fields = GetMappedFields(indexName, language)
                        }
                    }

                };

                var result = _httpClientHelper.Post(new Uri(uri), Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(matchQuery)));
                response = Encoding.UTF8.GetString(result);
            }
            else
            {
                string query = CreateSearchQuery(searchText, type);
                uri += $"?q={query}&size={size}";
                if(_serverInfo.Version >= Constants.TotalHitsAsIntAddedVersion)
                {
                    uri += "&rest_total_hits_as_int=true";
                }

                response = _httpClientHelper.GetString(new Uri(uri));
            }

            dynamic parsedResponse = JObject.Parse(response);
            JArray hits = parsedResponse.hits.hits;

            return hits.Select(h => new InspectItem(h)).ToList();
        }

        private string[] GetMappedFields(string indexName, string language)
        {
            var config = ElasticSearchSection.GetConfiguration();
            var nameWithoutLanguage = indexName.Substring(0, indexName.Length - language.Length - 1);
            var index = config.IndicesParsed.Single(i => i.Name == nameWithoutLanguage);
            var mappingType = String.IsNullOrEmpty(index.Type) ? typeof(IndexItem) : Type.GetType(index.Type);
            var mapping = _mapping.GetIndexMapping(mappingType, language, indexName);
            return mapping.Properties.Select(p => p.Key).ToArray();
        }

        public Dictionary<string, List<TypeCount>> GetTypes(string searchText, string indexName)
        {
            string uri = $"{_elasticSearchSettings.Host}/{indexName}/_search";
            if(_serverInfo.Version >= Constants.TotalHitsAsIntAddedVersion)
            {
                uri += "?rest_total_hits_as_int=true";
            }

            object query = CreateTypeQuery(searchText);
            string json = JsonConvert.SerializeObject(query, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            byte[] data = Encoding.UTF8.GetBytes(json);
            byte[] returnData = _httpClientHelper.Post(new Uri(uri), data);
            string response = Encoding.UTF8.GetString(returnData);

            dynamic agg = JObject.Parse(response);
            JArray buckets = agg.aggregations.typesAgg.buckets;

            return buckets
                .Select(ToTypeCount)
                .GroupBy(x => x.Group)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private static string CreateSearchQuery(string searchText, string type)
        {
            string query = null;

            if(!String.IsNullOrEmpty(searchText))
            {
                query = searchText;
            }

            if(!String.IsNullOrEmpty(type))
            {
                if(query != null)
                {
                    query += " AND ";
                }

                query += "Type:" + type;
            }

            return query;
        }

        private static object CreateTypeQuery(string searchText)
        {
            var aggsQuery = new
            {
                typesAgg = new
                {
                    terms = new
                    {
                        field = "Type.keyword",
                        size = 200
                    }
                }
            };

            object searchQuery = null;
            if(!String.IsNullOrEmpty(searchText))
            {
                searchQuery = new
                {
                    query_string = new
                    {
                        query = searchText
                    }
                };
            }

            var query = new
            {
                aggs = aggsQuery,
                query = searchQuery,
                size = 0
            };

            return query;
        }

        private TypeCount ToTypeCount(JToken instance)
        {
            var typeCount = new TypeCount
            {
                Type = instance.Value<JObject>().Property("key").Value.ToString(),
                Count = Convert.ToInt32(instance.Value<JObject>().Property("doc_count").Value.ToString())
            };

            typeCount.ShortTypeName = typeCount.Type.GetShortTypeName();
            ContentType contentType = _contentTypeRepository.Load(typeCount.ShortTypeName);

            if(contentType != null)
            {
                typeCount.Name = contentType.LocalizedName;
                typeCount.Group = contentType.LocalizedGroupName;
            }

            if(String.IsNullOrWhiteSpace(typeCount.Name))
            {
                typeCount.Name = typeCount.ShortTypeName;
            }

            if(String.IsNullOrWhiteSpace(typeCount.Group))
            {
                typeCount.Group = " " + LocalizationExtensions.TranslateWithPath("nocategory", "/epinovaelasticsearch/indexinspector/");
            }

            return typeCount;
        }
    }
}