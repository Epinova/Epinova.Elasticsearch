using System.Configuration;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Initialization.Internal;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(PlugInInitialization))]
    public class TrackingInitializer : IInitializableModule
    {
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
                    CONSTRAINT [PK_ElasticTracking] PRIMARY KEY CLUSTERED ( [Query] ASC, [Language] ASC )";

                DbHelper.CreateTable(connectionString, Constants.TrackingTable, definition);
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