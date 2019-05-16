using System;
using Epinova.ElasticSearch.Core.EPiServer;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using EPiServer.Core;
using Moq;
using TestData;
using Xunit;

namespace Core.Episerver.Tests
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class IndexerTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly Indexer _indexer;

        private readonly ServiceLocatorFixture _fixture;

        public IndexerTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;
            _indexer = new Indexer(
                fixture.ServiceLocationMock.CoreIndexerMock.Object,
                fixture.ServiceLocationMock.SettingsMock.Object,
                fixture.ServiceLocationMock.ContentLoaderMock.Object);
        }

        [Fact]
        public void Update_TypeDecoratedWithExcludeAttribute_IsNotIndexed()
        {
            var excludedType = new TypeWithExcludeAttribute();

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.Update(excludedType);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(
                m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), null),
                Times.Never());

            Assert.Equal(IndexingStatus.ExcludedByConvention, result);
        }

        [Fact]
        public void Update_TypeWithHideFromSearchProperty_IsNotIndexed()
        {
            var hiddenType = new TypeWithHideFromSearchProperty();

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.Update(hiddenType);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(
                m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), null),
                Times.Never());

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);
        }

        [Fact]
        public void Update_TypeWithHideFromSearchProperty_DeletesFromIndex()
        {
            var hiddenType = new TypeWithHideFromSearchProperty
            {
                ContentLink = new ContentReference(123)
            };

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            _indexer.Update(hiddenType);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Delete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type>(), null),
                Times.Once);
        }

        [Theory]
        [InlineData(PageShortcutType.External)]
        [InlineData(PageShortcutType.Inactive)]
        [InlineData(PageShortcutType.Shortcut)]
        public void Update_SkipsShortcutTypesOtherThanNormalAndFetchData(PageShortcutType type)
        {
            PageData page = Factory.GetPageData();
            page.LinkType = type;

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void Update_IncludesShortcutTypeNormal()
        {
            TestPage page = Factory.GetTestPage();

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.Ok, result);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Once);
        }

        [Fact]
        public void Update_HasHideFromSearch_IsExcluded()
        {
            TestPage page = Factory.GetTestPage();
            page.Property["HidefromSearch"] = new PropertyBoolean { Value = true };

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void Update_HasDeletedStatus_IsExcluded()
        {
            TestPage page = Factory.GetTestPage();
            page.Property["PageDeleted"] = new PropertyBoolean { Value = true };

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void FormsUpload_IsNotIndexed()
        {
            TestMedia media = Factory.GetMediaData("foo", "jpg");
            TestPage page = Factory.GetTestPage();

            var assetHelperMock = new Mock<ContentAssetHelper>();
            assetHelperMock
                .Setup(m => m.GetAssetOwner(media.ContentLink))
                .Returns(page);

            _fixture.ServiceLocationMock.ServiceLocatorMock
                .Setup(m => m.GetInstance<ContentAssetHelper>())
                .Returns(assetHelperMock.Object);

            IndexingStatus result = _indexer.Update(media);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);
        }
    }
}
