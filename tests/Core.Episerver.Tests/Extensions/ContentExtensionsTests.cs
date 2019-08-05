using System;
using System.Collections.Generic;
using System.Globalization;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Serialization;
using EPiServer.Core;
using Moq;
using TestData;
using Xunit;

namespace Core.Episerver.Tests.Extensions
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class ContentExtensionsTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly IContent _content;
        private readonly ServiceLocatorFixture _fixture;

        public ContentExtensionsTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;

            _content = Factory.GetPageData<PageData>(
                id: 100,
                parentId: 200,
                language: new CultureInfo("no"));

            _content.ContentLink = new ContentReference(100);
            _content.ParentLink = new ContentReference(200);
            _content.Name = "Foo";
        }

        [Theory]
        [InlineData(DefaultFields.Id, 100)]
        [InlineData(DefaultFields.ParentLink, 200)]
        [InlineData(DefaultFields.Name, "Foo")]
        [InlineData(DefaultFields.Type, "EPiServer_Core_PageData")]
        [InlineData(DefaultFields.Lang, "no")]
        public void AsIndexItem_SetsStandardFields(string propName, object expectedValue)
        {
            dynamic result = _content.AsIndexItem();
            var dictionary = (IDictionary<string, object>)result;

            Assert.Equal(expectedValue, dictionary[propName]);
        }

        [Fact]
        public void AsIndexItem_SetsTypeHierarchy()
        {
            dynamic result = _content.AsIndexItem();
            var dictionary = (IDictionary<string, object>)result;
            var collection = (ICollection<string>)dictionary[DefaultFields.Types];

            Assert.Contains("EPiServer_Core_PageData", collection);
            Assert.Contains("EPiServer_Core_IVersionable", collection);
        }

        [Theory]
        [InlineData(DefaultFields.Id)]
        [InlineData(DefaultFields.Indexed)]
        [InlineData(DefaultFields.Name)]
        [InlineData(DefaultFields.Type)]
        [InlineData(DefaultFields.Types)]
        [InlineData(DefaultFields.ParentLink)]
        [InlineData(DefaultFields.StartPublish)]
        [InlineData(DefaultFields.StopPublish)]
        [InlineData(DefaultFields.Path)]
        [InlineData(DefaultFields.Created)]
        [InlineData(DefaultFields.Changed)]
        [InlineData(DefaultFields.Acl)]
        public void AsIndexItem_AppendsDefaultFields(string field)
        {
            dynamic result = _content.AsIndexItem();
            var dictionary = (IDictionary<string, object>)result;

            Assert.True(dictionary.ContainsKey(field));
        }

        [Theory]
        [InlineData(DefaultFields.Lang)]
        [InlineData(DefaultFields.BestBets)]
        [InlineData(nameof(TestPage.TestProp))]
        [InlineData(nameof(TestPage.StemmedProp))]
        [InlineData(nameof(TestPage.TestIntProp))]
        [InlineData(nameof(TestPage.TestDateProp))]
        [InlineData(nameof(TestPage.TestDecimalProp))]
        public void AsIndexItem_AppendsIndexableProperties(string field)
        {
            var testPage = Factory.GetPageData<TestPage>();
            testPage.TestDateProp = DateTime.Now;
            testPage.TestDecimalProp = 1.23m;
            testPage.TestIntProp = Factory.GetInteger();
            testPage.TestProp = Factory.GetString();
            testPage.StemmedProp = Factory.GetString();

            dynamic result = testPage.AsIndexItem();
            var dictionary = (IDictionary<string, object>)result;

            Assert.True(dictionary.ContainsKey(field));
        }

        [Fact]
        public void AsIndexItem_AppendsCustomProperties()
        {
            Indexing.Instance.ForType<TestPage>().IncludeField(x => x.CustomStuff());

            var testPage = Factory.GetPageData<TestPage>();

            dynamic result = testPage.AsIndexItem();
            var dictionary = (IDictionary<string, object>)result;

            Assert.True(dictionary.ContainsKey("CustomStuff"));
        }

        [Theory]
        [InlineData("jpg", true)]
        [InlineData("exe", true)]
        [InlineData("pdf", false)]
        [InlineData("docx", false)]
        public void IsBinary_ReturnsCorrectFlag(string ext, bool expected)
        {
            var result = ContentExtensions.IsBinary(ext);

            Assert.Equal(expected, result);
        }

        [Fact(Skip = "Shared state issue")]
        public void GetContentResults_ReturnsHits()
        {
            var hits = new[]
            {
                new SearchHit(new Hit { Source = new IndexItem { Id = Factory.GetInteger(), Name = Factory.GetString() } }),
                new SearchHit(new Hit { Source = new IndexItem { Id = Factory.GetInteger(), Name = Factory.GetString() } }),
                new SearchHit(new Hit { Source = new IndexItem { Id = Factory.GetInteger(), Name = Factory.GetString() } })
            };

            IContent content = Factory.GetPageData();

            _fixture.ServiceLocationMock.ContentLoaderMock
                .Setup(m => m.TryGet(It.IsAny<ContentReference>(), out content))
                .Returns(true);

            _fixture.ServiceLocationMock.ServiceMock
                .Setup(m => m.GetResults(It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
                .Returns(new SearchResult { Hits = hits });

            var result = _fixture.ServiceLocationMock.ServiceMock.Object.GetContentResults();

            Assert.NotEmpty(result.Hits);
        }
    }
}
