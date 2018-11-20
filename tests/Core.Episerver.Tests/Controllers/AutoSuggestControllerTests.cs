using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using EPiServer;
using EPiServer.DataAbstraction;
using Moq;
using Xunit;

namespace Core.Episerver.Tests.Controllers
{
    public class AutoSuggestControllerTests
    {
        private readonly ElasticAutoSuggestController _controller;
        private readonly Mock<IAutoSuggestRepository> _autoSuggestRepositoryMock;

        public AutoSuggestControllerTests()
        {
            var contentLoaderMock = new Mock<IContentLoader>();
            _autoSuggestRepositoryMock = new Mock<IAutoSuggestRepository>();

            var languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            languageBranchRepositoryMock
                .Setup(m => m.ListEnabled())
                .Returns(new List<LanguageBranch> {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
                });

            _controller = new ElasticAutoSuggestController(
                contentLoaderMock.Object,
                languageBranchRepositoryMock.Object,
                _autoSuggestRepositoryMock.Object);
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
