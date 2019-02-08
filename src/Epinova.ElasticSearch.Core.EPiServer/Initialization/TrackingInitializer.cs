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

            if (!DbHelper.TableExists(connectionString, Constants.TrackingTable))
            {
                const string definition = @"
                    [Query] [nvarchar](400) NOT NULL,
                    [Searches] [int] NOT NULL,
                    [NoHits] [bit] NOT NULL,
                	[Language] [nvarchar](10) NOT NULL,
                	[IndexName] [nvarchar](200) NOT NULL";

                Logger.Information("Creating tracking table");
                
                DbHelper.CreateTable(connectionString, Constants.TrackingTable, definition);
            }

            if (!DbHelper.ColumnExists(connectionString, Constants.TrackingTable, Constants.TrackingFieldIndex))
            {
                Logger.Information($"Extending table with {Constants.TrackingFieldIndex} column");

                DbHelper.ExecuteCommand(connectionString, $"ALTER TABLE {Constants.TrackingTable} ADD {Constants.TrackingFieldIndex} nvarchar(200)");
                DbHelper.ExecuteCommand(connectionString, $"IF (OBJECT_ID('PK_ElasticTracking', 'U') IS NOT NULL) BEGIN ALTER TABLE {Constants.TrackingTable}  DROP CONSTRAINT [PK_ElasticTracking] END" );
            }
        }

        public void Preload(string[] parameters)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}