using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Utilities;
using TestData;
using NUnit.Framework;
using EPiServer.ServiceLocation;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Models.Admin;

namespace Integration.Tests
{
    [TestFixture]
    public class ServerTests
    {
        [SetUp]
        public void Setup()
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var indexing = new Indexing(settings);
            var index = new Index(settings, ElasticFixtureSettings.IndexName);

            if (!indexing.IndexExists(ElasticFixtureSettings.IndexName))
                index.Initialize(ElasticFixtureSettings.IndexType);

            index.WaitForStatus(20);
        }


        [TestCase("ingest-attachment", 5)]
        [TestCase("mapper-attachments", 2)]
        public void Server_HasRequiredPlugins(string name, int minVersion)
        {
            Plugin plugin = Server.Plugins.First(p => p.Component.ToLower() == name);
            Assert.True(plugin.Version.Major >= minVersion);
        }


        [Test]
        public void Server_HasMinimumVersion()
        {
            Assert.True(Server.Info.Version.Major >= 2);
        }
    }
}
