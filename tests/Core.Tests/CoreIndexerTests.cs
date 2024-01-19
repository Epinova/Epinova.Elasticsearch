using System;
using System.Globalization;
using System.IO;
using System.Net;
using Epinova.ElasticSearch.Core;
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
                .Setup(m => m.GetDefaultIndexName(new CultureInfo("de")))
                .Returns("my-index");

            _fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.GetDefaultIndexName(new CultureInfo("sv")))
                .Returns("delete-me");

            _fixture.ServiceLocationMock.HttpClientMock
                .Setup(m => m.Head(new Uri("http://example.com/bad-index")))
                .Returns(HttpStatusCode.NotFound);

            _fixture.ServiceLocationMock.HttpClientMock
                .Setup(m => m.Head(new Uri("http://example.com/my-index")))
                .Returns(HttpStatusCode.OK);

            _coreIndexer = new CoreIndexer(
                _fixture.ServiceLocationMock.ServerInfoMock.Object,
                _fixture.ServiceLocationMock.SettingsMock.Object,
                _fixture.ServiceLocationMock.HttpClientMock.Object);
        }

        [Fact]
        public void Bulk_EmptyOperations_ReturnsEmptyResult()
        {
            var result = _coreIndexer.Bulk(Array.Empty<BulkOperation>());
            Assert.Empty(result.Batches);
        }

        [Fact]
        public void Bulk_NullOperations_ReturnsEmptyResult()
        {
            var result = _coreIndexer.Bulk(null);
            Assert.Empty(result.Batches);
        }

        [Fact]
        public void Bulk_CallsClientPost()
        {
            var id = Factory.GetInteger();
            string indexName = _fixture.ServiceLocationMock.SettingsMock.Object.GetCustomIndexName("my-index", new CultureInfo("en"));
            _coreIndexer.Bulk(new BulkOperation(indexName, new { Foo = "bar" }, Operation.Index, null, id));

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Post(new Uri($"http://example.com/_bulk?pipeline={Epinova.ElasticSearch.Core.Pipelines.Attachment.Name}"), It.IsAny<Stream>()), Times.AtLeastOnce);
        }

        [Fact]
        public void Bulk_ReturnsBatchResults()
        {
            string indexName = _fixture.ServiceLocationMock.SettingsMock.Object.GetCustomIndexName("my-index", new CultureInfo("en"));
            var result = _coreIndexer.Bulk(new BulkOperation(indexName, new { Foo = 42 }, Operation.Index, null, 123));
            Assert.NotEmpty(result.Batches);
        }

        [Fact]
        public void Delete_CallsClientHeadAndDelete()
        {
            var id = Factory.GetInteger();
            _coreIndexer.Delete(id, new CultureInfo("sv"));
            var uri = new Uri($"http://example.com/delete-me/_doc/{id}");

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Head(uri), Times.Once);
            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Delete(uri), Times.Once);
        }

        [Fact]
        public void Update_CallsClientPut()
        {
            var id = Factory.GetInteger();
            _coreIndexer.Update(id, new { Foo = 42 }, "my-index");

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Put(new Uri($"http://example.com/my-index/_doc/{id}"), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void Update_WithTypes_CallsClientPut()
        {
            var id = Factory.GetInteger();
            _coreIndexer.Update(id, new { Foo = 42, Types = new[] { "foo", "bar" } }, "my-index");

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Put(new Uri($"http://example.com/my-index/_doc/{id}"), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void Update_RefreshesIndex()
        {
            var id = Factory.GetInteger();
            _coreIndexer.Update(id, new { Foo = 42 }, "my-index");

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.GetString(new Uri("http://example.com/my-index/_refresh")), Times.AtLeastOnce);
        }

        [Fact]
        public void Update_CallsBeforeUpdateItemEvent()
        {
            var id = Factory.GetInteger();
            bool eventCalled = false;
            _coreIndexer.BeforeUpdateItem += _ => eventCalled = true;
            _coreIndexer.Update(id, new { Foo = 42 }, "my-index");
            Assert.True(eventCalled);
        }

        [Fact]
        public void ClearBestBets_FiresAfterUpdateBestBetEvent()
        {
            var id = Factory.GetInteger();
            bool eventCalled = false;
            _coreIndexer.AfterUpdateBestBet += _ => eventCalled = true;
            _coreIndexer.ClearBestBets("my-index", id);
            Assert.True(eventCalled);
        }

        [Fact]
        public void ClearBestBets_CallsClientPost()
        {
            var id = Factory.GetInteger();
            _coreIndexer.ClearBestBets("my-index", id);

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Post(new Uri($"http://example.com/my-index/_update/{id}/"), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void UpdateBestBets_FiresAfterUpdateBestBetEvent()
        {
            var id = Factory.GetInteger();
            bool eventCalled = false;
            _coreIndexer.AfterUpdateBestBet += _ => eventCalled = true;
            _coreIndexer.UpdateBestBets("my-index", id, new[] { "foo", "bar" });
            Assert.True(eventCalled);
        }

        [Fact]
        public void UpdateBestBets_CallsClientPost()
        {
            var id = Factory.GetInteger();
            _coreIndexer.ClearBestBets("my-index", id);

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.Post(new Uri($"http://example.com/my-index/_update/{id}/"), It.IsAny<byte[]>()), Times.Once);
        }

        [Fact]
        public void UpdateBestBets_RefreshesIndex()
        {
            var id = Factory.GetInteger();
            _coreIndexer.UpdateBestBets("my-index", id, new[] { "foo", "bar" });

            _fixture.ServiceLocationMock.HttpClientMock
                .Verify(m => m.GetString(new Uri("http://example.com/my-index/_refresh")), Times.AtLeastOnce);
        }

        //TODO: Tests for UpdateMapping and CreateX
    }
}
