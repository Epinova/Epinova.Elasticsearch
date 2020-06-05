using System.Collections.Generic;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using EPiServer.Core;
using Moq;
using TestData;
using Xunit;

namespace Core.Episerver.Tests
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class QueryExtensionsTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ServiceLocatorFixture _fixture;
        private readonly ElasticSearchService _serviceStub;

        public QueryExtensionsTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;
            _serviceStub = new ElasticSearchService(
                fixture.ServiceLocationMock.ServerInfoMock.Object,
                fixture.ServiceLocationMock.SettingsMock.Object,
                fixture.ServiceLocationMock.HttpClientMock.Object);
        }

        [Fact]
        public void StartFrom_UsesContentReferenceId()
        {
            var contentLink = new ContentReference(42);
            var instance = _serviceStub.StartFrom(contentLink);

            Assert.Equal(42, instance.RootId);
        }

        [Fact]
        public void Exclude_UsesContentReferenceId()
        {
            var contentLink = new ContentReference(42);
            _serviceStub.Exclude(contentLink);
            Assert.Contains(42, _serviceStub.ExcludedRoots.Keys);
        }

        [Fact]
        public void Exclude_UsesContentId()
        {
            var contentMock = new Mock<IContent>();
            contentMock
                .Setup(m => m.ContentLink)
                .Returns(new ContentReference(42));

            _serviceStub.Exclude(contentMock.Object);
            Assert.Contains(42, _serviceStub.ExcludedRoots.Keys);
        }

        [Fact]
        public void BoostByAncestor_UsesContentReferenceId()
        {
            var contentLink = new ContentReference(42);
            _serviceStub.BoostByAncestor(contentLink, 2);
            Assert.Contains(42, _serviceStub.BoostAncestors.Keys);
        }

        [Fact]
        public void GetSuggestions_ReturnsHits()
        {
            var repoMock = new Mock<IAutoSuggestRepository>();
            repoMock.Setup(m => m.GetWords(It.IsAny<string>()))
                .Returns(new List<string>());

            _fixture.ServiceLocationMock.ServiceLocatorMock
                .Setup(m => m.GetInstance<IAutoSuggestRepository>())
                .Returns(repoMock.Object);

            var engine = new TestableSearchEngine(
                new[] { "foo", "bar", "baz" },
                _fixture.ServiceLocationMock.ServerInfoMock.Object,
                _fixture.ServiceLocationMock.SettingsMock.Object,
                _fixture.ServiceLocationMock.HttpClientMock.Object);

            var results = _serviceStub.GetSuggestions("foo", engine);
            Assert.Contains("foo", results);
        }
    }
}