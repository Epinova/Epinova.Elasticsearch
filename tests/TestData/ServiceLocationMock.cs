using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using Moq;

namespace TestData
{
    public class ServiceLocationMock
    {
        public Mock<IServiceLocator> ServiceLocatorMock { get; set; }
        public Mock<IIndexer> IndexerMock { get; set; }
        public Mock<ICoreIndexer> CoreIndexerMock { get; set; }
        public Mock<ISynonymRepository> SynonymRepositoryMock { get; set; }
        public Mock<IContentLoader> ContentLoaderMock { get; set; }
        public Mock<IContentIndexService> ContentIndexServiceMock { get; set; }
        public Mock<IElasticSearchSettings> SettingsMock { get; set; }
        public Mock<IServerInfoService> ServerInfoMock { get; set; }
        public Mock<IHttpClientHelper> HttpClientMock { get; set; }
        public Mock<IElasticSearchService<IContent>> ServiceMock { get; set; }
        public Mock<ILanguageBranchRepository> LanguageBranchRepositoryMock { get; set; }
        public Mock<IPublishedStateAssessor> StateAssesorMock { get; set; }
        public Mock<IScheduledJobRepository> ScheduledJobRepositoryMock { get; set; }
        public Mock<IScheduledJobExecutor> ScheduledJobExecutorMock { get; set; }
        public ITemplateResolver TemplateResolver { get; set; }
    }
}
