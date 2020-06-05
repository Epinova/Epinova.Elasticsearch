using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Routing;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;
using Moq;
using TestData;
using Xunit;

namespace Core.Episerver.Tests.Controllers.Abstractions
{
    public class ElasticSearchControllerBaseTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ControllerStub _controller;
        private readonly Mock<ILanguageBranchRepository> _languageBranchRepositoryMock;
        private readonly Mock<ActionExecutingContext> _actionExecutingContextMock;

        public ElasticSearchControllerBaseTests(ServiceLocatorFixture fixture)
        {
            fixture.MockInfoEndpoints();

            _actionExecutingContextMock = new Mock<ActionExecutingContext>();

            _languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            _languageBranchRepositoryMock
                .Setup(m => m.ListEnabled())
                .Returns(new List<LanguageBranch> {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
                });

            _controller = new ControllerStub(
                fixture.ServiceLocationMock.ServerInfoMock.Object,
                fixture.ServiceLocationMock.SettingsMock.Object,
                fixture.ServiceLocationMock.HttpClientMock.Object,
                _languageBranchRepositoryMock.Object);
        }

        [Fact]
        public void Ctor_PopulatesLanguages() => Assert.NotEmpty(_controller.Languages);

        [Fact]
        public void Ctor_PopulatesIndices() => Assert.NotEmpty(_controller.Indices);

        [Fact]
        public void OnActionExecuting_FallsBackToFirstLanguage()
        {
            _controller.OnActionExecuting(_actionExecutingContextMock.Object);

            Assert.Equal(_controller.CurrentLanguage, "en");
        }

        private class ControllerStub : ElasticSearchControllerBase
        {
            public ControllerStub(
                IServerInfoService serverInfoService,
                IElasticSearchSettings settings,
                IHttpClientHelper httpClientHelper,
                ILanguageBranchRepository languageBranchRepository)
                : base(serverInfoService, settings, httpClientHelper, languageBranchRepository)
            {
            }

            public new string CurrentIndex => base.CurrentIndex;

            public new string CurrentLanguage => base.CurrentLanguage;

            public new Dictionary<string, string> Languages => base.Languages;

            public new List<IndexInformation> Indices => base.Indices;

            public new void OnActionExecuting(ActionExecutingContext filterContext)
                => base.OnActionExecuting(filterContext);

            public new void OnResultExecuting(ResultExecutingContext filterContext)
                => base.OnResultExecuting(filterContext);

            public new void Initialize(RequestContext requestContext)
                => base.Initialize(requestContext);
        }
    }
}
