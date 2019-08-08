using System.Globalization;
using Epinova.ElasticSearch.Core.EPiServer.Events;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Security;
using Moq;
using TestData;
using Xunit;
using static TestData.Factory;

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
            _fixture.ServiceLocationMock.IndexerMock.Reset();

            IndexingEvents.DeleteFromIndex(null, null);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Delete(It.IsAny<ContentReference>()), Times.Never);
        }

        [Fact]
        public void DeleteFromIndex_ValidContentlink_CallsDelete()
        {
            var input = GetDeleteScenario();

            _fixture.ServiceLocationMock.IndexerMock.Reset();

            IndexingEvents.DeleteFromIndex(null, input.Args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Delete(input.Content.ContentLink), Times.Once);
        }

        [Fact]
        public void UpdateIndex_PageInWasteBasket_CallsDelete()
        {
            var input = GetMoveToWasteBasketScenario();

            _fixture.ServiceLocationMock.IndexerMock.Reset();

            IndexingEvents.UpdateIndex(null, input.Args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Delete(input.Content.ContentLink), Times.Once);
            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(input.Content, null), Times.Never);
        }

        [Fact]
        public void UpdateIndex_MoveWithDescendents_UpdatesAll()
        {
            var child1 = Factory.GetPageData();
            var child2 = Factory.GetPageData();
            var input = GetMoveScenario(child1.ContentLink, child2.ContentLink);

            _fixture.ServiceLocationMock.ContentLoaderMock
                .Setup(m => m.GetItems(new[] { child1.ContentLink, child2.ContentLink }, It.IsAny<CultureInfo>()))
                .Returns(new[] { child1, child2 });

            _fixture.ServiceLocationMock.IndexerMock.Reset();

            IndexingEvents.UpdateIndex(null, input.Args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(input.Content, null), Times.Once);
            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(child1, null), Times.Once);
            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(child2, null), Times.Once);
        }

        [Fact]
        public void UpdateIndex_PublishEvent_CallsUpdate()
        {
            var input = GetPublishScenario();

            _fixture.ServiceLocationMock.IndexerMock.Reset();

            IndexingEvents.UpdateIndex(null, input.Args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(input.Content, null), Times.Once);
        }

        [Fact(Skip = "Shared state issue in CI")]
        public void UpdateIndex_AclChangeOnPublishedContent_CallsUpdate()
        {
            var link = Factory.GetPageReference();
            var args = new ContentSecurityEventArg(link, new ContentAccessControlList(), SecuritySaveType.None);

            var versionRepoMock = new Mock<IContentVersionRepository>();
            versionRepoMock
                .Setup(m => m.LoadPublished(link))
                .Returns(new ContentVersion(default, default, default, default, default, default, default, default, default, default));

            _fixture.ServiceLocationMock.ServiceLocatorMock
                .Setup(m => m.GetInstance<IContentVersionRepository>())
                .Returns(versionRepoMock.Object);

            IContent dummy = null;
            _fixture.ServiceLocationMock.ContentLoaderMock
                .Setup(m => m.TryGet(link, out dummy))
                .Returns(true);

            _fixture.ServiceLocationMock.IndexerMock.Reset();

            IndexingEvents.UpdateIndex(null, args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(dummy, null), Times.Once);
        }

        [Fact]
        public void UpdateIndex_AclChangeOnUnpublishedContent_DoesNotCallUpdate()
        {
            var link = Factory.GetPageReference();
            var content = Factory.GetPageData(id: link.ID);
            var args = new ContentSecurityEventArg(link, new ContentAccessControlList(), SecuritySaveType.None);

            _fixture.ServiceLocationMock.IndexerMock.Reset();

            IndexingEvents.UpdateIndex(null, args);

            _fixture.ServiceLocationMock.IndexerMock.Verify(m => m.Update(content, null), Times.Never);
        }
    }
}
