using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using EPiServer.DataAbstraction;
using Moq;
using TestData;
using Xunit;

namespace Core.Episerver.Tests.Controllers
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class ElasticAdminControllerTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ElasticAdminController _controller;

        public ElasticAdminControllerTests(ServiceLocatorFixture fixture)
        {
            fixture.MockInfoEndpoints();

            var indexerMock = new Mock<ICoreIndexer>();

            var languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            languageBranchRepositoryMock
                .Setup(m => m.ListEnabled())
                .Returns(new List<LanguageBranch> {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
                });

            _controller = new ElasticAdminController(
                fixture.ServiceLocationMock.ContentIndexServiceMock.Object,
                languageBranchRepositoryMock.Object,
                indexerMock.Object,
                fixture.ServiceLocationMock.SettingsMock.Object,
                fixture.ServiceLocationMock.HttpClientMock.Object,
                fixture.ServiceLocationMock.ServerInfoMock.Object,
                fixture.ServiceLocationMock.ScheduledJobRepositoryMock.Object,
                fixture.ServiceLocationMock.ScheduledJobExecutorMock.Object);
        }

        [Fact]
        public void Index_GetsIndices()
        {
            var result = _controller.Index() as ViewResult;
            var model = result.Model as AdminViewModel;

            Assert.NotEmpty(model.AllIndexes);
        }

        [Fact]
        public void Index_GetsNodeInfo()
        {
            var result = _controller.Index() as ViewResult;
            var model = result.Model as AdminViewModel;

            Assert.NotEmpty(model.NodeInfo);
        }

        [Fact]
        public void Index_GetsClusterHealth()
        {
            var result = _controller.Index() as ViewResult;
            var model = result.Model as AdminViewModel;

            Assert.NotNull(model.ClusterHealth);
        }
    }
}
