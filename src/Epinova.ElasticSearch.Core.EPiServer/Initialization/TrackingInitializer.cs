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
                	[IndexName] [nvarchar](255) NULL,
                    CONSTRAINT [PK_ElasticTracking] PRIMARY KEY CLUSTERED ( [Query] ASC, [Language] ASC )";

                Logger.Information("Creating tracking table");
                
                DbHelper.CreateTable(connectionString, Constants.TrackingTable, definition);
            }

            if (!DbHelper.ColumnExists(connectionString, Constants.TrackingTable, Constants.TrackingFieldIndex))
            {
                Logger.Information($"Extending table with {Constants.TrackingFieldIndex} column");
                
                DbHelper.AddColumn(connectionString, Constants.TrackingTable, Constants.TrackingFieldIndex, "nvarchar(255)");
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