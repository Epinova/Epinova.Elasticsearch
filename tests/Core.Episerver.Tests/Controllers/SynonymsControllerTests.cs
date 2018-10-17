using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using EPiServer;
using EPiServer.DataAbstraction;
using Moq;
using Xunit;
using TestData;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Models.Admin;

namespace Core.Episerver.Tests.Controllers
{
    public class SynonymsControllerTests
    {
        private readonly ElasticSynonymsController _controller;
        private readonly Mock<ISynonymRepository> _synonymRepositoryMock;

        public SynonymsControllerTests()
        {
            Factory.SetupServiceLocator(testHost: "http://example.com");

            var contentLoaderMock = new Mock<IContentLoader>();

            _synonymRepositoryMock = new Mock<ISynonymRepository>();
            _synonymRepositoryMock
                .Setup(m => m.GetSynonyms(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new List<Synonym>());

            var languageBranchRepositoryMock = new Mock<ILanguageBranchRepository>();
            languageBranchRepositoryMock
                .Setup(m => m.ListEnabled())
                .Returns(new List<LanguageBranch> {
                    new LanguageBranch(new CultureInfo("en")),
                    new LanguageBranch(new CultureInfo("no"))
                });

            var settingsMock = new Mock<IElasticSearchSettings>();

            var indexMock = new Mock<Index>(settingsMock.Object, Factory.GetString());
            indexMock
                .Setup(m => m.GetIndices())
                .Returns(new[] {
                    new IndexInformation { Index = "" },
                });

            _controller = new ElasticSynonymsController(
                contentLoaderMock.Object,
                _synonymRepositoryMock.Object,
                languageBranchRepositoryMock.Object,
                indexMock.Object);
        }

        [Theory]
        [InlineData("foo", "")]
        [InlineData("", "bar")]
        [InlineData(null, null)]
        public void Add_MissingInput_DoesNothing(string from, string to)
        {
            _controller.Add(new Synonym {From = from, To = to, TwoWay = false}, "", "", "");

            _synonymRepositoryMock.Verify(m => m.GetSynonyms("", null), Times.Never);
        }

        [Theory]
        [InlineData("foo", "bar", true)]
        [InlineData("omg", "lol", false)]
        public void Add_ValidInput_CallsRepository(string from, string to, bool twoway)
        {
           _controller.Add(new Synonym{From = from, To = to, TwoWay = twoway}, "", "", "");

            _synonymRepositoryMock.Verify(m => m.SetSynonyms("", "", It.IsAny<List<Synonym>>(), ""), Times.Once);
        }

        [Fact]
        public void Index_GetsEnabledLanguages()
        {
            var result = _controller.Index() as ViewResult;
            var model = result.Model as SynonymsViewModel;

            Assert.Contains(model.SynonymsByLanguage, l => l.LanguageId == "en");
            Assert.Contains(model.SynonymsByLanguage, l => l.LanguageId == "no");
        }

        [Theory]
        [InlineData("foo", "bar", false)]
        [InlineData("lol", "baz", true)]
        public void Delete_RemovesSynonym(string from, string to, bool twoway)
        {
            var synonyms = new List<Synonym>
            {
                new Synonym {From = "foo", To = "bar", TwoWay = false},
                new Synonym {From = "lol", To = "baz", TwoWay = true},
                new Synonym {From = "aaa", To = "bbb", TwoWay = false}
            };

            var expected = new List<Synonym>(synonyms);
            string synonymFrom = from + (twoway ? null : "=>" + from);
            expected.RemoveAll(s => s.From == synonymFrom);

            _synonymRepositoryMock
                .Setup(m => m.GetSynonyms(It.IsAny<string>(), ""))
                .Returns(synonyms);

            _controller.Delete(new Synonym { From = from, To = to, TwoWay = twoway }, "", "", "");

            _synonymRepositoryMock.Verify(m => m.SetSynonyms("", "", expected, ""), Times.Once);
        }
    }
}
