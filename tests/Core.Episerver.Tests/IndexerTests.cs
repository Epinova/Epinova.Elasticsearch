using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core.EPiServer;
using Epinova.ElasticSearch.Core.EPiServer.Enums;
using Epinova.ElasticSearch.Core.Models.Bulk;
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
        public void Delete_ContentReference_CallsCoreDelete()
        {
            var contentLink = Factory.GetPageReference();

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            _indexer.Delete(contentLink);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(
                m => m.Delete(contentLink.ID.ToString(), It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<string>()),
                Times.Once());
        }

        [Fact]
        public void Delete_IConent_CallsCoreDelete()
        {
            var page = Factory.GetPageData();

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            _indexer.Delete(page, "test");

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(
                m => m.Delete(page.ContentLink.ID.ToString(), It.IsAny<string>(), It.IsAny<Type>(), It.IsAny<string>()),
                Times.Once());
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
        public void Update_ContentExcludedByRoot_IsNotIndexed()
        {
            var id = Factory.GetInteger();

            Epinova.ElasticSearch.Core.Conventions.Indexing.Instance.ExcludeRoot(id);

            var page = Factory.GetPageData(id: id);

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.ExcludedByConvention, result);
        }

        [Fact]
        public void Update_ContentWithParentExcludedByRoot_IsNotIndexed()
        {
            var parentId = Factory.GetInteger();

            Epinova.ElasticSearch.Core.Conventions.Indexing.Instance.ExcludeRoot(parentId);

            var page = Factory.GetTestPage(parentId: parentId);

            IndexingStatus result = _indexer.Update(page);

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
            var page = Factory.GetTestPage();
            page.Property["PageDeleted"] = new PropertyBoolean { Value = true };

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.Update(page);

            Assert.Equal(IndexingStatus.HideFromSearchProperty, result);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()),
                Times.Never);
        }

        [Fact]
        public void UpdateStructure_UpdatesSelfAndChildren()
        {
            var page = Factory.GetPageData();
            var children = new[]
            {
                Factory.GetPageData(),
                Factory.GetPageData(),
                Factory.GetPageData()
            };
            var childrenLinks = children.Select(x => x.ContentLink).ToArray();

            _fixture.ServiceLocationMock.ContentLoaderMock
                .Setup(m => m.GetDescendents(page.ContentLink))
                .Returns(childrenLinks);

            _fixture.ServiceLocationMock.ContentLoaderMock
                .Setup(m => m.GetItems(childrenLinks, It.IsAny<CultureInfo>()))
                .Returns(children);

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            IndexingStatus result = _indexer.UpdateStructure(page);

            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(page.ContentLink.ID.ToString(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()), Times.Once);
            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(childrenLinks[0].ID.ToString(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()), Times.Once);
            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(childrenLinks[1].ID.ToString(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()), Times.Once);
            _fixture.ServiceLocationMock.CoreIndexerMock.Verify(m => m.Update(childrenLinks[2].ID.ToString(), It.IsAny<object>(), It.IsAny<string>(), It.IsAny<Type>()), Times.Once);
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

        [Fact]
        public void BulkUpdate_CallsCoreBulk()
        {
            var batch = new[]
            {
                Factory.GetTestPage(),
                Factory.GetTestPage(),
                Factory.GetTestPage()
            };

            _fixture.ServiceLocationMock.CoreIndexerMock.Reset();

            _fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.GetDefaultIndexName(It.IsAny<string>()))
                .Returns("test");

            var result = _indexer.BulkUpdate(batch, null, null);

            _fixture.ServiceLocationMock.CoreIndexerMock
                .Verify(m => m.Bulk(It.IsAny<IEnumerable<BulkOperation>>(), It.IsAny<Action<string>>()), Times.Once);
        }
    }
}
