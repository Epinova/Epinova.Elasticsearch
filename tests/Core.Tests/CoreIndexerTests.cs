using System;
using System.Net;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models.Bulk;
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
            _fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.GetDefaultIndexName("de"))
                .Returns("my-index");

            _fixture.ServiceLocationMock.HttpClientMock
                .Setup(m => m.Head(new Uri("http://example.com/bad-index")))
                .Returns(HttpStatusCode.NotFound);

            _fixture.ServiceLocationMock.HttpClientMock
                .Setup(m => m.Head(new Uri("http://example.com/my-index")))
                .Returns(HttpStatusCode.OK);

            _coreIndexer = new CoreIndexer(
                _fixture.ServiceLocationMock.SettingsMock.Object,
                _fixture.ServiceLocationMock.HttpClientMock.Object);
        }

        [Fact]
        public void Bulk_NoOperations_ReturnsEmptyResult()
        {
            var result = _coreIndexer.Bulk(new BulkOperation[0]);
            Assert.Empty(result.Batches);
        }

        [Fact]
        public void Bulk_CallsClientPost()
        {
            var id = Factory.GetInteger().ToString();
            _coreIndexer.Bulk(new BulkOperation(new { Foo = "bar" }, "en", id, "my-index"));

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Post(new Uri($"http://example.com/_bulk?pipeline={Epinova.ElasticSearch.Core.Pipelines.Attachment.Name}"), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void Bulk_ReturnsBatchResults()
        {
            var result = _coreIndexer.Bulk(new BulkOperation(new { Foo = 42 }, "en", "123", "my-index"));
            Assert.NotEmpty(result.Batches);
        }

        [Fact]
        public void Delete_CallsClientHeadAndDelete()
        {
            var id = Factory.GetInteger().ToString();
            _coreIndexer.Delete(id, "en", typeof(TestPage), "delete-me");
            var uri = new Uri($"http://example.com/delete-me/{typeof(TestPage).GetTypeName()}/{id}");

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Head(uri), Times.Once);
            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Delete(uri), Times.Once);
        }

        [Fact]
        public void Update_UnknownIndex_Throws()
        {
            var id = Factory.GetInteger().ToString();
            Assert.Throws<InvalidOperationException>(()
                => _coreIndexer.Update(id, new { Foo = 42 }, "bad-index"));
        }

        [Fact]
        public void Update_CallsClientPut()
        {
            var id = Factory.GetInteger().ToString();
            _coreIndexer.Update(id, new { Foo = 42 }, "my-index");

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Put(It.IsAny<Uri>(), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void Update_RefreshesIndex()
        {
            var id = Factory.GetInteger().ToString();
            _coreIndexer.Update(id, new { Foo = 42 }, "my-index");

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.GetString(new Uri("http://example.com/my-index/_refresh")), Times.AtLeastOnce);
        }

        [Fact]
        public void Update_CallsBeforeUpdateItemEvent()
        {
            var id = Factory.GetInteger().ToString();
            bool eventCalled = false;
            CoreIndexer.BeforeUpdateItem += _ => eventCalled = true;
            _coreIndexer.Update(id, new { Foo = 42 }, "my-index");
            Assert.True(eventCalled);
        }

        [Fact]
        public void ClearBestBets_FiresAfterUpdateBestBetEvent()
        {
            var id = Factory.GetInteger().ToString();
            bool eventCalled = false;
            CoreIndexer.AfterUpdateBestBet += _ => eventCalled = true;
            _coreIndexer.ClearBestBets("my-index", typeof(TestPage), id);
            Assert.True(eventCalled);
        }

        [Fact]
        public void ClearBestBets_CallsClientPost()
        {
            var id = Factory.GetInteger().ToString();
            _coreIndexer.ClearBestBets("my-index", typeof(TestPage), id);

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Post(new Uri($"http://example.com/my-index/{typeof(TestPage).GetTypeName()}/{id}/_update"), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void UpdateBestBets_FiresAfterUpdateBestBetEvent()
        {
            var id = Factory.GetInteger().ToString();
            bool eventCalled = false;
            CoreIndexer.AfterUpdateBestBet += _ => eventCalled = true;
            _coreIndexer.UpdateBestBets("my-index", typeof(TestPage), id, new[] { "foo", "bar" });
            Assert.True(eventCalled);
        }

        [Fact]
        public void UpdateBestBets_CallsClientPost()
        {
            var id = Factory.GetInteger().ToString();
            _coreIndexer.ClearBestBets("my-index", typeof(TestPage), id);

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Post(new Uri($"http://example.com/my-index/{typeof(TestPage).GetTypeName()}/{id}/_update"), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void UpdateBestBets_RefreshesIndex()
        {
            var id = Factory.GetInteger().ToString();
            _coreIndexer.UpdateBestBets("my-index", typeof(TestPage), id, new[] { "foo", "bar" });

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.GetString(new Uri("http://example.com/my-index/_refresh")), Times.AtLeastOnce);
        }

        //TODO: Tests for UpdateMapping
    }
}
