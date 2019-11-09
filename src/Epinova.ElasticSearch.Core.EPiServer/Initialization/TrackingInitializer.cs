using System.Configuration;
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

                DbHelper.ExecuteCommand(connectionString, $"ALTER TABLE {Constants.Tracking.TableName} ADD {Constants.TrackingFieldIndex} nvarchar(200)");
                DbHelper.ExecuteCommand(connectionString, $"IF (OBJECT_ID('PK_ElasticTracking', 'U') IS NOT NULL) BEGIN ALTER TABLE {Constants.Tracking.TableName}  DROP CONSTRAINT [PK_ElasticTracking] END");
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Not applicable
        }
    }
}