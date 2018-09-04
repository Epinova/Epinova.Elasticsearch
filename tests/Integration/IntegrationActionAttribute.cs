using System;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using TestData;

namespace Integration.Tests
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Assembly, AllowMultiple = true)]
    public class IntegrationActionAttribute : Attribute, ITestAction
    {
        public void BeforeTest(ITest test)
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var indexing = new Indexing(settings);
            var index = new Index(settings, ElasticFixtureSettings.IndexName);

            WriteToConsole("Before", test);
            if (indexing.IndexExists(ElasticFixtureSettings.IndexName))
                return;

            Console.WriteLine("Creating index: " + ElasticFixtureSettings.IndexName);
            index.Initialize(ElasticFixtureSettings.IndexType);
            index.WaitForStatus(20);
        }

        public void AfterTest(ITest test)
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var indexing = new Indexing(settings);
            indexing.DeleteIndex(ElasticFixtureSettings.IndexName);

            WriteToConsole("After", test);
            Console.WriteLine("Deleting index: " + ElasticFixtureSettings.IndexName + " => " + !indexing.IndexExists(ElasticFixtureSettings.IndexName));
        }

        public ActionTargets Targets => ActionTargets.Test | ActionTargets.Suite;

        private static void WriteToConsole(string eventMessage, ITest details)
        {
            Console.WriteLine("{0} {1}: {2}",
                eventMessage,
                details.IsSuite ? "Suite" : "Case",
                details.Name);
        }
    }
}
