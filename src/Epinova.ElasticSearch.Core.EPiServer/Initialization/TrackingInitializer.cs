using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Initialization.Internal;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(PlugInInitialization))]
    public class TrackingInitializer : IInitializableModule
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(TrackingInitializer));

        public void Initialize(InitializationEngine context)
        {
            string connectionString = ConfigurationManager.ConnectionStrings[Constants.EPiServerConnectionStringName].ConnectionString;

            if(!DbHelper.TableExists(connectionString, Constants.Tracking.TableName))
            {
                Logger.Information("Creating tracking table");
                DbHelper.CreateTable(connectionString, Constants.Tracking.TableName, Constants.Tracking.Sql.Definition);
            }

            if(!DbHelper.ColumnExists(connectionString, Constants.Tracking.TableName, Constants.TrackingFieldIndex))
            {
                Logger.Information($"Extending table with {Constants.TrackingFieldIndex} column");
                DbHelper.ExecuteCommand(connectionString, $"ALTER TABLE {Constants.Tracking.TableName} ADD {Constants.TrackingFieldIndex} nvarchar(200) NOT NULL");
            }
            else if(DbHelper.ColumnIsNullable(connectionString, Constants.Tracking.TableName, Constants.TrackingFieldIndex))
            {
                DbHelper.AdjustColumnNullable(connectionString, Constants.Tracking.TableName, Constants.TrackingFieldIndex, "nvarchar(200)", isNullable: false);
            }

            FixPrimaryKeys(connectionString);
        }

        private void FixPrimaryKeys(string connectionString)
        {
            string primaryKeySql = $"SELECT c.COLUMN_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS t JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE c ON c.CONSTRAINT_NAME= t.CONSTRAINT_NAME WHERE c.TABLE_NAME='{Constants.Tracking.TableName}' and t.CONSTRAINT_TYPE='PRIMARY KEY'";
            IEnumerable<string> primaryKeyColumns = DbHelper.ExecuteReader(connectionString, primaryKeySql)?.Select(r => Convert.ToString(r["COLUMN_NAME"])) ?? Enumerable.Empty<string>();
            IEnumerable<string> missingPrimaryKeys = new List<string> { "Query", "Language", "IndexName" }.Except(primaryKeyColumns);
            
            if(missingPrimaryKeys.Any())
            {
                DbHelper.ExecuteCommand(connectionString, $"ALTER TABLE {Constants.Tracking.TableName} DROP CONSTRAINT[PK_ElasticTracking]");
                DbHelper.ExecuteCommand(connectionString, $"ALTER TABLE {Constants.Tracking.TableName} ADD CONSTRAINT[PK_ElasticTracking] PRIMARY KEY([Query], [Language], [IndexName])");
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Not applicable
        }
    }
}