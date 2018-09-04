using System;
using System.Configuration;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using Integration.Tests;
using NUnit.Framework;
using TestData;

[SetUpFixture]
[IntegrationAction]
// ReSharper disable once CheckNamespace
public class IntegrationFixture
{
    public static ServiceLocationMock ServiceLocationMock;
    private IElasticSearchSettings _settings;
    private Indexing _indexing;
    private Index _index;

    [OneTimeSetUp]
    public void Setup()
    {
        string host = ConfigurationManager.AppSettings["IntegrationServerUrl"];

        ElasticFixtureSettings.Language = "no";
        ElasticFixtureSettings.IndexName = "integration-tests-" + Guid.NewGuid().ToString("N") + "-" + ElasticFixtureSettings.Language;
    
        Console.WriteLine("Starting integration tests");
        Console.WriteLine("Host: " + host);
        Console.WriteLine("Index: " + ElasticFixtureSettings.IndexName);


        ServiceLocationMock = Factory.SetupServiceLocator(host);

        Epinova.ElasticSearch.Core.Conventions.Indexing.Instance
            .IncludeFileType("docx")
            .IncludeFileType("pdf");

        _settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        _indexing = new Indexing(_settings);
        _index = new Index(_settings, ElasticFixtureSettings.IndexName);

        if (!_indexing.IndexExists(_settings.Index))
            _index.Initialize(ElasticFixtureSettings.IndexType);

        _index.WaitForStatus(20);

    }

    [OneTimeTearDown]
    public void TearDown()
    {
        Console.WriteLine("Ending integration tests");
        Console.WriteLine("Deleting 'integration-tests-*' indices");
        _indexing.DeleteIndex("integration-tests-*");
    }
}
