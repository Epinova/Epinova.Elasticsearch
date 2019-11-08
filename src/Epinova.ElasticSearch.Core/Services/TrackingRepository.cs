using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.Services
{
    [ServiceConfiguration(ServiceType = typeof(ITrackingRepository), Lifecycle = ServiceInstanceScope.Transient)]
    public class TrackingRepository : ITrackingRepository
    {
        private static readonly string ConnectionString = GetConnectionString();

        public void AddSearch(string languageId, string text, bool noHits, string index)
        {
            text = text ?? String.Empty;
            if(text.Length > 200)
            {
                text = text.Substring(0, 200);
            }

            if(SearchExists(text, languageId, index))
            {
                DbHelper.ExecuteCommand(
                    ConnectionString, 
                    Constants.Tracking.Sql.Update, 
                    new Dictionary<string, object>
                    {
                        {"@query", text},
                        {"@lang", languageId},
                        {"@index", index}
                    });

                return;
            }

            DbHelper.ExecuteCommand(
                ConnectionString,
                Constants.Tracking.Sql.Insert,
                new Dictionary<string, object>
                {
                    {"@query", text},
                    {"@nohits", noHits ? 1 : 0},
                    {"@lang", languageId},
                    {"@index", index}
                });
        }

        public void Clear(string languageId, string index)
        {
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

        private bool SearchExists(string text, string languageId, string index)
        {
            var results = DbHelper.ExecuteReader(
                ConnectionString, 
                Constants.Tracking.Sql.Exists,
                new Dictionary<string, object>
                {
                    {"@query", text},
                    {"@lang", languageId},
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