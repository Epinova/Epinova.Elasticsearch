using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using System.Web.Routing;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.EPiServer.Controllers.Abstractions;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.DataAbstraction;
using Moq;
using Xunit;

namespace Core.Episerver.Tests.Controllers.Abstractions
{
    public class ElasticSearchControllerBaseTests
    {
        private ControllerStub _controller;
        private readonly Mock<ILanguageBranchRepository> _languageBranchRepositoryMock;
        private readonly Mock<Index> _indexHelperMock;
        private readonly Mock<ActionExecutingContext> _actionExecutingContextMock;

        public ElasticSearchControllerBaseTests()
        {
            _actionExecutingContextMock = new Mock<ActionExecutingContext>();

            var settingsMock = new Mock<IElasticSearchSettings>();

            _indexHelperMock = new Mock<Index>(settingsMock.Object);
            _indexHelperMock.Setup(m => m.GetIndices()).Returns(new[] { new IndexInformation { Index = "Foo" } });

            _languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            _languageBranchRepositoryMock
                .Setup(m => m.ListEnabled())
                .Returns(new List<LanguageBranch> {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
                });
        }

        [Fact]
        public void Ctor_PopulatesLanguages()
        {
            _controller = new ControllerStub(_indexHelperMock.Object, _languageBranchRepositoryMock.Object);

            Assert.NotEmpty(_controller.Languages);
        }

        [Fact]
        public void Ctor_PopulatesIndices()
        {
            _controller = new ControllerStub(_indexHelperMock.Object, _languageBranchRepositoryMock.Object);

            Assert.NotEmpty(_controller.Indices);
        }

        [Fact]
        public void OnActionExecuting_FallsBackToFirstLanguage()
        {
            _controller = new ControllerStub(_indexHelperMock.Object, _languageBranchRepositoryMock.Object);
            _controller.OnActionExecuting(_actionExecutingContextMock.Object);

            Assert.Equal(_controller.CurrentLanguage, "en");
        }

        private class ControllerStub : ElasticSearchControllerBase
        {
            public ControllerStub(Index indexHelper, ILanguageBranchRepository languageBranchRepository) : base(indexHelper, languageBranchRepository)
            {
            }

            public ControllerStub(IElasticSearchSettings settings, ILanguageBranchRepository languageBranchRepository) : base(settings, languageBranchRepository)
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
