using Epinova.ElasticSearch.Core.EPiServer.Events;
using EPiServer.Core;
using Moq;
using static TestData.Factory;
using Xunit;
using TestData;

namespace Core.Episerver.Tests.Events
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class IndexingEventsTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ServiceLocatorFixture _fixture;

        public IndexingEventsTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public void DeleteFromIndex_MissingContentlink_DoesNotCallDelete()
        {
            IndexingEvents.DeleteFromIndex(null, null);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Delete(It.IsAny<ContentReference>()), Times.Never);
        }

        [Fact]
        public void DeleteFromIndex_ValidContentlink_CallsDelete()
        {
            var input = GetPublishScenario();

            IndexingEvents.DeleteFromIndex(null, input.Args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Delete(input.Content.ContentLink), Times.Once);
        }

        [Fact]
        public void UpdateIndex_PageInWasteBasket_CallsDelete()
        {
            var input = GetPublishScenario();
            input.Args.TargetLink = ContentReference.WasteBasket;

            IndexingEvents.UpdateIndex(null, input.Args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Delete(input.Content.ContentLink), Times.Once);
            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(input.Content, null), Times.Never);
        }

        [Fact]
        public void UpdateIndex_PublishEvent_CallsUpdate()
        {
            var input = GetPublishScenario();

            IndexingEvents.UpdateIndex(null, input.Args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(input.Content, null), Times.Once);
        }
    }
}
