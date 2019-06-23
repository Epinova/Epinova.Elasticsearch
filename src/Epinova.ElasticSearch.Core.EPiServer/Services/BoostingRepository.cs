using System;
using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.Extensions;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Cache;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Services
{
    [ServiceConfiguration(ServiceType = typeof(IBoostingRepository), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class BoostingRepository : IBoostingRepository
    {
        private readonly IContentRepository _contentRepository;
        private readonly IContentCacheKeyCreator _cacheKeyCreator;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(BoostingRepository));

        public BoostingRepository(
            IContentCacheKeyCreator cacheKeyCreator,
            IContentRepository contentRepository)
        {
            _contentRepository = contentRepository;
            _cacheKeyCreator = cacheKeyCreator;
        }


        public Dictionary<string, int> GetByType(Type type)
        {
            BoostingData[] boostingData = GetBoostingData(type);

            Dictionary<string, int> boosting = new Dictionary<string, int>();

            foreach(var boost in boostingData)
            {
                string fieldName = boost.FieldName;

                if(!boosting.ContainsKey(fieldName))
                {
                    boosting.Add(fieldName, boost.Weight);
                }
                else
                {
                    boosting[fieldName] = Math.Max(boost.Weight, boosting[fieldName]);
                }
            }

            return boosting;
        }


        public void Save(string typeName, Dictionary<string, int> boosting)
        {
            DeleteBoostingData(typeName);

            foreach(KeyValuePair<string, int> dataPair in boosting)
            {
                Logger.Debug($"Saving boosting for type '{typeName}'. Values: {dataPair.Key + ":" + dataPair.Value}");

                BoostingData dataContent = _contentRepository.GetDefault<BoostingData>(GetBoostingFolder());
                dataContent.Name = typeName;
                dataContent.FieldName = dataPair.Key;
                dataContent.Weight = dataPair.Value;
                _contentRepository.Save(dataContent, SaveAction.Publish, AccessLevel.NoAccess);
            }

            CacheManager.Remove(GetCacheKey(typeName));
        }

        public void DeleteAll()
        {
            ContentReference boostingFolder = GetBoostingFolder();

            foreach(BoostingData data in (_contentRepository.GetChildren<BoostingData>(boostingFolder) ?? Enumerable.Empty<BoostingData>()).ToArray())
            {
                Logger.Information($"Deleting boosting: {data.FieldName}->{data.Weight}");
                _contentRepository.Delete(data.ContentLink, true, AccessLevel.NoAccess);
            }
        }


        private ContentReference GetBoostingFolder(string folderName = "Elasticsearch Boosting")
        {
            ContentReference parent = ContentReference.RootPage;

            var boostingFolder = _contentRepository.GetChildren<BoostingFolder>(parent).FirstOrDefault();
            if(boostingFolder == null)
            {
                boostingFolder = _contentRepository.GetDefault<BoostingFolder>(parent);
                boostingFolder.Name = folderName;
                _contentRepository.Save(boostingFolder, SaveAction.Publish, AccessLevel.NoAccess);
            }
            return boostingFolder.ContentLink;
        }

        private BoostingData[] GetBoostingData(Type type)
        {
            string cacheKey = GetCacheKey(type);

            if(CacheManager.Get(cacheKey) is BoostingData[] cache)
            {
                return cache;
            }

            ContentReference boostingFolder = GetBoostingFolder();

            BoostingData[] boostingData = (_contentRepository.GetChildren<BoostingData>(boostingFolder) ?? Enumerable.Empty<BoostingData>()).ToArray();

            if(TypeIsValid(type))
            {
                boostingData = boostingData
                    .Where(s => s.Name.Equals(type.GetTypeName(), StringComparison.OrdinalIgnoreCase))
                    .ToArray();
            }

            CacheManager.Insert(cacheKey, boostingData, GetEvictionPolicy());

            return boostingData;
        }

        private CacheEvictionPolicy GetEvictionPolicy()
        {
            return new CacheEvictionPolicy(new[]
            {
                _cacheKeyCreator.VersionKey
            });
        }

        private static bool TypeIsValid(Type type)
        {
            return !type.IsAbstract
                && !type.IsInterface
                && !type.IsGenericType
                && !(type.FullName ?? String.Empty).StartsWith("EPISERVER", StringComparison.OrdinalIgnoreCase);
        }

        private void DeleteBoostingData(string typeName)
        {
            ContentReference boostingFolder = GetBoostingFolder();

            foreach(BoostingData data in
                    (_contentRepository.GetChildren<BoostingData>(boostingFolder) ?? Enumerable.Empty<BoostingData>())
                        .Where(s => s.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                        .ToArray())
            {
                Logger.Debug($"Deleting boosting for type '{typeName}'. Value: {data.Name + ":" + data.Weight}");

                _contentRepository.Delete(data.ContentLink, true, AccessLevel.NoAccess);
            }

            CacheManager.Remove(GetCacheKey(typeName));
        }

        private static string GetCacheKey(Type type)
            => GetCacheKey(type.FullName);

        private static string GetCacheKey(string typeName)
            => String.Concat("BoostingRepository.GetBoostingData.", typeName);
    }
}