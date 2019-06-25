using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
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
            var indexerMock = new Mock<ICoreIndexer>();
            var healthMock = new Mock<Health>(
                fixture.ServiceLocationMock.SettingsMock.Object,
                fixture.ServiceLocationMock.HttpClientMock.Object);

            healthMock.Setup(m => m.GetClusterHealth()).Returns(new HealthInformation());
            healthMock.Setup(m => m.GetNodeInfo()).Returns(new[] { new Node() });

            var languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            languageBranchRepositoryMock
                .Setup(m => m.ListEnabled())
                .Returns(new List<LanguageBranch> {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
                });

            var indexHelperMock = new Mock<Index>(
                new Mock<IElasticSearchSettings>().Object);

            indexHelperMock.Setup(m => m.GetIndices()).Returns(new[] { new IndexInformation { Index = "foo" } });

            _controller = new ElasticAdminController(
                languageBranchRepositoryMock.Object,
                indexerMock.Object,
                fixture.ServiceLocationMock.SettingsMock.Object,
                indexHelperMock.Object,
                healthMock.Object);
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
