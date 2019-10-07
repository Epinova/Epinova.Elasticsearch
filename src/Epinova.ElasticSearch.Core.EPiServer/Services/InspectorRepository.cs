using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
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

        internal InspectorRepository()
        {
            _elasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        }

        public InspectorRepository(IElasticSearchSettings settings, IHttpClientHelper httpClientHelper, IContentTypeRepository contentTypeRepository)
        {
            _elasticSearchSettings = settings;
            _contentTypeRepository = contentTypeRepository;
            _httpClientHelper = httpClientHelper;
        }

        public List<InspectItem> Search(string searchText, string indexName, int size, string type = null, string selectedIndex = null)
        {
            if(String.IsNullOrWhiteSpace(searchText) && String.IsNullOrWhiteSpace(type))
            {
                return new List<InspectItem>();
            }

            string query = CreateSearchQuery(searchText, type);
            string uri = $"{_elasticSearchSettings.Host}/{indexName}/_search?q={query}&size={size}";
            if(Server.Info.Version.Major >= 7)
            {
                uri += "&rest_total_hits_as_int=true";
            }

            string response = _httpClientHelper.GetString(new Uri(uri));
            dynamic parsedResponse = JObject.Parse(response);
            JArray hits = parsedResponse.hits.hits;

            return hits.Select(h => new InspectItem(h)).ToList();
        }

        public Dictionary<string, List<TypeCount>> GetTypes(string searchText, string indexName)
        {
            string uri = $"{_elasticSearchSettings.Host}/{indexName}/_search";
            if(Server.Info.Version.Major >= 7)
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

        private string GetIndexName(string languageId, string selectedIndex = null)
        {
            return selectedIndex != null
                ? _elasticSearchSettings.GetCustomIndexName(selectedIndex, languageId)
                : _elasticSearchSettings.GetDefaultIndexName(languageId);
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