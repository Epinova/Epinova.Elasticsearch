using System.Collections.Generic;
using System.Globalization;
using Epinova.ElasticSearch.Core;
using EPiServer.Core;
using TestData;
using Xunit;
using Epinova.ElasticSearch.Core.EPiServer.Extensions;

namespace Core.Episerver.Tests.Extensions
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class ContentExtensionsTests : IClassFixture<ServiceLocatorFixture>
    {
        private readonly IContent _content;

        public ContentExtensionsTests()
        {
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

        [Fact]
        public void AsIndexItem_SetsTypeHierarchy()
        {
            dynamic result = _content.AsIndexItem();
            var dictionary = (IDictionary<string, object>)result;
            var collection = (ICollection<string>)dictionary[DefaultFields.Types];

            Assert.Contains("EPiServer_Core_PageData", collection);
            Assert.Contains("EPiServer_Core_IVersionable", collection);
        }
    }
}
