using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.Services
{
    [ServiceConfiguration(ServiceType = typeof(ITrackingRepository), Lifecycle = ServiceInstanceScope.Transient)]
    public class TrackingRepository : ITrackingRepository
    {
        private readonly IElasticSearchSettings _settings;
        private static readonly string ConnectionString = GetConnectionString();
        
        public TrackingRepository(IElasticSearchSettings settings)
        {
            _settings = settings;
        }

        public void AddSearch<T>(IElasticSearchService<T> service, bool noHits)
        {
            string text = service.SearchText ?? String.Empty;
            if(text.Length > 200)
                text = text.Substring(0, 200);

            string index = _settings.GetIndexNameWithoutLanguage(service.IndexName);
            if(SearchExists(text, service.SearchLanguage, index))
            {
                DbHelper.ExecuteCommand(
                    ConnectionString, 
                    Constants.Tracking.Sql.Update, 
                    new Dictionary<string, object>
                    {
                        {"@query", text},
                        {"@lang", service.SearchLanguage.Name},
                        {"@index", index}
                    });

                return;
            }

            DbHelper.ExecuteCommand(ConnectionString, Constants.Tracking.Sql.Insert,
                new Dictionary<string, object>
                {
                    {"@query", text},
                    {"@nohits", noHits ? 1 : 0},
                    {"@lang", service.SearchLanguage.Name},
                    {"@index", index}
                });
        }

        public void Clear(string languageId, string index)
        {
            index = _settings.GetIndexNameWithoutLanguage(index);

            DbHelper.ExecuteCommand(
                ConnectionString,
                Constants.Tracking.Sql.Delete,
                new Dictionary<string, object>
                {
                    {"@lang", languageId},
                    {"@index", index}
                });
        }

        public IEnumerable<Tracking> GetSearches(string languageId, string index)
        {
            index = _settings.GetIndexNameWithoutLanguage(index);

            var results = DbHelper.ExecuteReader(
                ConnectionString, 
                Constants.Tracking.Sql.Select,
                new Dictionary<string, object>
                {
                    {"@lang", languageId},
                    {"@index", index}
                });

            return results.Select(r => new Tracking
            {
                Query = Convert.ToString(r["Query"])?.Replace('\\', ' '),
                Searches = Convert.ToInt64(r["Searches"])
            });
        }

        public IEnumerable<Tracking> GetSearchesWithoutHits(string languageId, string index)
        {
            index = _settings.GetIndexNameWithoutLanguage(index);

            var results = DbHelper.ExecuteReader(
                ConnectionString, 
                Constants.Tracking.Sql.SelectNoHits,
                new Dictionary<string, object>
                {
                    {"@lang", languageId},
                    {"@index", index}
                });

            return results.Select(r => new Tracking
            {
                Query = Convert.ToString(r["Query"])?.Replace('\\', ' '),
                Searches = Convert.ToInt64(r["Searches"])
            });
        }

        private bool SearchExists(string text, CultureInfo language, string index)
        {
            index = _settings.GetIndexNameWithoutLanguage(index);

            var results = DbHelper.ExecuteReader(
                ConnectionString, 
                Constants.Tracking.Sql.Exists,
                new Dictionary<string, object>
                {
                    {"@query", text},
                    {"@lang", language.Name},
                    {"@index", index}
                });

            return results.Count > 0;
        }

        private static string GetConnectionString()
        {
            var config = ElasticSearchSection.GetConfiguration();
            var connectionStringName = String.IsNullOrWhiteSpace(config.TrackingConnectionStringName)
                    ? "EPiServerDB"
                    : config.TrackingConnectionStringName;

            return ConfigurationManager.ConnectionStrings?[connectionStringName]?.ConnectionString;
        }
    }
}