using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
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
    public class AutoSuggestControllerTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ElasticAutoSuggestController _controller;
        private readonly Mock<IAutoSuggestRepository> _autoSuggestRepositoryMock;

        public AutoSuggestControllerTests(ServiceLocatorFixture fixture)
        {
            fixture.MockInfoEndpoints();

            _autoSuggestRepositoryMock = new Mock<IAutoSuggestRepository>();

            var languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            languageBranchRepositoryMock
                .Setup(m => m.ListEnabled())
                .Returns(new List<LanguageBranch> {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
                });

            var indexHelperMock = new Mock<Index>(new Mock<IElasticSearchSettings>().Object);

            indexHelperMock.Setup(m => m.GetIndices()).Returns(Enumerable.Empty<IndexInformation>());

            _controller = new ElasticAutoSuggestController(
                languageBranchRepositoryMock.Object,
                _autoSuggestRepositoryMock.Object,
                fixture.ServiceLocationMock.SettingsMock.Object,
                fixture.ServiceLocationMock.ServerInfoMock.Object,
                fixture.ServiceLocationMock.HttpClientMock.Object);
        }

        [Fact]
        public void Index_GetsEnabledLanguages()
        {
            var result = _controller.Index() as ViewResult;
            var model = result.Model as AutoSuggestViewModel;

            Assert.Contains(model.WordsByLanguage, l => l.LanguageId == "en");
            Assert.Contains(model.WordsByLanguage, l => l.LanguageId == "no");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void AddWord_MissingInput_DoesNothing(string word)
        {
            _controller.AddWord("", word);

            _autoSuggestRepositoryMock.Verify(m => m.AddWord("", It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("høyesterettsjustitiarius")]
        public void AddWord_ValidInput_CallsRepository(string word)
        {
            _controller.AddWord("", word);
            _autoSuggestRepositoryMock.Verify(m => m.AddWord("", word), Times.Once);
        }

        [Theory]
        [InlineData("foo|bar")]
        [InlineData("høyesteretts|justitiarius")]
        public void AddWord_InputWithSeparator_StripsSeparator(string word)
        {
            string expected = word.Replace("|", String.Empty);
            _controller.AddWord("", word);
            _autoSuggestRepositoryMock.Verify(m => m.AddWord("", expected), Times.Once);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData("høyesterettsjustitiarius")]
        public void Delete_RemovesWord(string word)
        {
            _controller.DeleteWord("", word);

            _autoSuggestRepositoryMock.Verify(m => m.DeleteWord("", word), Times.Once);
        }
    }
}
