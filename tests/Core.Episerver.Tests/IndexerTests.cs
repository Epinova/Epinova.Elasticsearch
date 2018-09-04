using System;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using Moq;
using TestData;
using Xunit;

namespace Core.Episerver.Tests
{
    public class IndexerTests
    {
        private readonly Indexer _indexer;
        private readonly Mock<ICoreIndexer> _coreIndexerMock;
        private readonly ServiceLocationMock _serviceLocationMock;

        public IndexerTests()
        {
            _serviceLocationMock = Factory.SetupServiceLocator();
            var settingsMock = new Mock<IElasticSearchSettings>();
            var contentLoaderMock = new Mock<IContentLoader>();

            _coreIndexerMock = new Mock<ICoreIndexer>();

            _indexer = new Indexer(_coreIndexerMock.Object, settingsMock.Object, contentLoaderMock.Object);
        }

        [Fact]
        public void Update_TypeDecoratedWithExcludeAttribute_IsNotIndexed()
        {
            var excludedType = new TypeWithExcludeAttribute();

            IndexingStatus result = _indexer.Update(excludedType);

            _coreIndexerMock.Verify(
                m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), null),
                Times.Never());

            Assert.Equal(IndexingStatus.ExcludedByConvention, result);
        }

        [Fact]
        public void Update_TypeWithHideFromSearchProperty_IsNotIndexed()
        {
            var hiddenType = new TypeWithHideFromSearchProperty();

            IndexingStatus result = _indexer.Update(hiddenType);

            _coreIndexerMock.Verify(
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

            _indexer.Update(hiddenType);

            _coreIndexerMock.Verify(m => m.Delete(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type>(), null), 
                Times.Once);
        }

        [Theory]
        [InlineData(PageShortcutType.External)]
        [InlineData(PageShortcutType.FetchData)]
        [InlineData(PageShortcutType.Inactive)]
        [InlineData(PageShortcutType.Shortcut)]
        public void Update_SkipsShortcutTypesOtherThanNormal(PageShortcutType type)
        {
            PageData page = Factory.GetPageData();
            page.LinkType = type;

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _coreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void Update_IncludesShortcutTypeNormal()
        {
            TestPage page = Factory.GetTestPage();

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.Ok, result);

            _coreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Once);
        }

        [Fact]
        public void Update_HasExpired_IsExcluded()
        {
            TestPage page = Factory.GetTestPage();
            page.Property["PageStopPublish"] = new PropertyDate { Value = DateTime.Now.AddDays(-30) };

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _coreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void Update_HasHideFromSearch_IsExcluded()
        {
            TestPage page = Factory.GetTestPage();
            page.Property["HidefromSearch"] = new PropertyBoolean { Value = true };

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _coreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void Update_HasDeletedStatus_IsExcluded()
        {
            TestPage page = Factory.GetTestPage();
            page.Property["PageDeleted"] = new PropertyBoolean { Value = true };

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _coreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void FormsUpload_IsNotIndexed()
        {
            TestMedia media = Factory.GetMediaData("foo", "jpg");
            TestPage page = Factory.GetTestPage();

            Indexer.FormsUploadNamespace = "TestData.ITestPage";

            var assetHelperMock = new Mock<ContentAssetHelper>();
            assetHelperMock
                .Setup(m => m.GetAssetOwner(media.ContentLink))
                .Returns(page);

            _serviceLocationMock.ServiceLocatorMock.Setup(m => m.GetInstance<ContentAssetHelper>()).Returns(assetHelperMock.Object);


            IndexingStatus result = _indexer.Update(media);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);
        }
    }
}
