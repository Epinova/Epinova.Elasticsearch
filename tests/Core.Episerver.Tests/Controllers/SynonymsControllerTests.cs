﻿using System.Collections.Generic;
using System.Globalization;
using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels;
using EPiServer.DataAbstraction;
using Moq;
using Xunit;
using TestData;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Admin;
using Epinova.ElasticSearch.Core.Models.Admin;
using System.Linq;

namespace Core.Episerver.Tests.Controllers
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class SynonymsControllerTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ServiceLocatorFixture _fixture;
        private readonly ElasticSynonymsController _controller;

        public SynonymsControllerTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;

            var indexHelperMock = new Mock<Index>(
                _fixture.ServiceLocationMock.SettingsMock.Object);

            indexHelperMock
                .Setup(m => m.GetIndices())
                .Returns(Enumerable.Empty<IndexInformation>());

            _controller = new ElasticSynonymsController(
                indexHelperMock.Object,
                _fixture.ServiceLocationMock.LanguageBranchRepositoryMock.Object,
                _fixture.ServiceLocationMock.SynonymRepositoryMock.Object);
        }

        [Theory]
        [InlineData("foo", "")]
        [InlineData("", "bar")]
        [InlineData(null, null)]
        public void Add_MissingInput_DoesNothing(string from, string to)
        {
            _controller.Add(new Synonym {From = from, To = to, TwoWay = false}, "", "", "");

            _fixture.ServiceLocationMock.SynonymRepositoryMock.Verify(m => m.GetSynonyms("", null), Times.Never);
        }

        [Theory]
        [InlineData("foo", "bar", true)]
        [InlineData("omg", "lol", false)]
        public void Add_ValidInput_CallsRepository(string from, string to, bool twoway)
        {
            _fixture.ServiceLocationMock.SynonymRepositoryMock.Invocations.Clear();

           _controller.Add(new Synonym{From = from, To = to, TwoWay = twoway}, "", "", "");

            _fixture.ServiceLocationMock.SynonymRepositoryMock
                .Verify(m => m.SetSynonyms("", "", It.IsAny<List<Synonym>>(), ""), Times.Once);
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
            var synonymFrom = from + (twoway ? null : "=>" + from);
            expected.RemoveAll(s => s.From == synonymFrom);

            _fixture.ServiceLocationMock.SynonymRepositoryMock
                .Setup(m => m.GetSynonyms(It.IsAny<string>(), ""))
                .Returns(synonyms);

            _controller.Delete(new Synonym { From = from, To = to, TwoWay = twoway }, "", "", "");

            _fixture.ServiceLocationMock.SynonymRepositoryMock.Verify(m => m.SetSynonyms("", "", expected, ""), Times.Once);
        }
    }
}
