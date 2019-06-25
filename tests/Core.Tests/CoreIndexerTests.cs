using System;
using Epinova.ElasticSearch.Core;
using Moq;
using TestData;
using Xunit;

namespace Core.Tests
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class CoreIndexerTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ServiceLocatorFixture _fixture;
        private readonly CoreIndexer _coreIndexer;

        public CoreIndexerTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;
            _coreIndexer = new CoreIndexer(
                _fixture.ServiceLocationMock.SettingsMock.Object,
                _fixture.ServiceLocationMock.HttpClientMock.Object);
        }

        [Fact]
        public void Delete_CallsClientHeadAndDelete()
        {
            _coreIndexer.Delete("42", "en", typeof(TestPage));

            _fixture.ServiceLocationMock.HttpClientMock.Verify(m => m.Head(It.IsAny<Uri>()), Times.Once);
            _fixture.ServiceLocationMock.HttpClientMock.Verify(m => m.Delete(It.IsAny<Uri>()), Times.Once);
        }
    }
}
