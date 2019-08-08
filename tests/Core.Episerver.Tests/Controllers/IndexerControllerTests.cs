using System.Web.Mvc;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Controllers;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using EPiServer;
using EPiServer.Core;
using Moq;
using TestData;
using Xunit;

namespace Core.Episerver.Tests.Controllers
{
    public class IndexerControllerTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly ElasticIndexerController _controller;
        private readonly Mock<IContentLoader> _contentLoaderMock;
        private readonly Mock<IIndexer> _indexerMock;

        public IndexerControllerTests()
        {
            _contentLoaderMock = new Mock<IContentLoader>();
            _indexerMock = new Mock<IIndexer>();

            _controller = new ElasticIndexerController(
                _contentLoaderMock.Object,
                _indexerMock.Object,
                null);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("foo")]
        public void UpdateItem_InvalidContentReference_ReturnsError(string id)
        {
            JsonResult response = _controller.UpdateItem(id);
            string result = response.Data.ToString();
            string error = "status = " + IndexingStatus.Error;
            Assert.Contains(error, result);
        }

        [Fact]
        public void UpdateItem_ValidContent_ReturnsOk()
        {
            IContent content = Factory.GetPageData();
            _contentLoaderMock.Setup(m => m.TryGet(It.IsAny<ContentReference>(), out content)).Returns(true);
            _indexerMock.Setup(m => m.Update(It.IsAny<IContent>(), null)).Returns(IndexingStatus.Ok);

            JsonResult response = _controller.UpdateItem("42");

            string result = response.Data.ToString();
            string ok = "status = " + IndexingStatus.Ok;
            Assert.Contains(ok, result);
        }
    }
}