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
        private static readonly string ConnectionString;

        static TrackingRepository()
        {
            var config = ElasticSearchSection.GetConfiguration();
            string connectionStringName = String.IsNullOrWhiteSpace(config.TrackingConnectionStringName)
                    ? "EPiServerDB"
                    : config.TrackingConnectionStringName;

            ConnectionString = ConfigurationManager.ConnectionStrings?[connectionStringName]?.ConnectionString;
        }


        public void AddSearch(string languageId, string text, bool noHits, string index)
        {
            text = text ?? String.Empty;
            if (text.Length > 200)
                text = text.Substring(0, 200);

            if (SearchExists(text, languageId, index))
            {
                string sql = $@"UPDATE [{Constants.TrackingTable}]
                    SET [Searches] = [Searches]+1
                    WHERE [Query] = @query AND [Language] = @lang AND [IndexName] = @index";

                var parameters = new Dictionary<string, object>
                {
                    {"@query", text},
                    {"@lang", languageId},
                    {"@index", index}
                };

                DbHelper.ExecuteCommand(ConnectionString, sql, parameters);
            }
            else
            {
                string sql = $@"INSERT INTO 
                    [{Constants.TrackingTable}] ([Query] ,[Searches], [NoHits], [Language], [IndexName])
                    VALUES (@query, 1, @nohits, @lang, @index)";

                var parameters = new Dictionary<string, object>
                {
                    {"@query", text},
                    {"@nohits", noHits ? 1 : 0},
                    {"@lang", languageId},
                    {"@index", index}
                };

                DbHelper.ExecuteCommand(ConnectionString, sql, parameters);
            }
        }

        public void Clear(string languageId, string index)
        {
            string sql = $@"DELETE FROM [{Constants.TrackingTable}] 
                WHERE Language = @lang AND [IndexName] = @index";

            var parameters = new Dictionary<string, object>
            {
                {"@lang", languageId},
                {"@index", index}
            };

            DbHelper.ExecuteCommand(ConnectionString, sql, parameters);
        }

        public IEnumerable<Tracking> GetSearches(string languageId, string index)
        {
            string sql = $@"SELECT [Query], [Searches]
                FROM [{Constants.TrackingTable}] 
                WHERE Language = @lang AND [IndexName] = @index";

            var parameters = new Dictionary<string, object>
            {
                {"@lang", languageId},
                {"@index", index}
            };

            var results = DbHelper.ExecuteReader(ConnectionString, sql, parameters);

            return results.Select(r => new Tracking
            {
                Query = Convert.ToString(r["Query"])?.Replace('\\', ' '),
                Searches = Convert.ToInt64(r["Searches"])
            });
        }

        public IEnumerable<Tracking> GetSearchesWithoutHits(string languageId, string index)
        {
            string sql = $@"SELECT [Query], [Searches] 
                FROM [{Constants.TrackingTable}] 
                WHERE Language = @lang AND NoHits=1 AND [IndexName] = @index";

            var parameters = new Dictionary<string, object>
            {
                {"@lang", languageId},
                {"@index", index}
            };

            var results = DbHelper.ExecuteReader(ConnectionString, sql, parameters);

            return results.Select(r => new Tracking
            {
                Query = Convert.ToString(r["Query"])?.Replace('\\', ' '),
                Searches = Convert.ToInt64(r["Searches"])
            });
        }


        private bool SearchExists(string text, string languageId, string index)
        {
            string sql = $@"SELECT Query 
                FROM [{Constants.TrackingTable}] 
                WHERE Query = @query AND Language = @lang AND [IndexName] = @index";

            var parameters = new Dictionary<string, object>
            {
                {"@query", text},
                {"@lang", languageId},
                {"@index", index}
            };

            var results = DbHelper.ExecuteReader(ConnectionString, sql, parameters);

            return results.Any();
        }
    }
}