using Epinova.ElasticSearch.Core.Utilities;
using Xunit;

namespace Core.Tests.Utilities
{
    public class TextUtilTests
    {
        [Theory]
        [InlineData("foo")]
        [InlineData("1-2")]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("123a")]
        [InlineData("000 123")]
        [InlineData("1 23")]
        [InlineData("0x123")]
        public void IsNumeric_NotANumber_ReturnsFalse(string instance)
        {
            bool result = TextUtil.IsNumeric(instance);

            Assert.False(result);
        }

        [Theory]
        [InlineData("0")]
        [InlineData("42")]
        [InlineData(System.Int64.MaxValue)]
        [InlineData(System.Int64.MinValue)]
        public void IsNumeric_Number_ReturnsTrue(string instance)
        {
            bool result = TextUtil.IsNumeric(instance);

            Assert.True(result);
        }

        [Fact]
        public void StripHtml_RemovesTags()
        {
            var instance = "<p><span>some text</span></p>";
            var result = TextUtil.StripHtml(instance);

            Assert.Contains("some text", result);
        }

        [Fact]
        public void StripHtml_DoesNotRemoveEntities()
        {
            var instance = "<p><span>some text &amp; more</span></p>";
            var result = TextUtil.StripHtml(instance);

            Assert.Contains("&amp;", result);
            Assert.DoesNotContain("text & more", result);
        }

        [Fact]
        public void StripHtmlAndEntities_RemovesTagsAndEntities()
        {
            var instance = "<p><span>some text &amp; stuff</span></p>";
            var result = TextUtil.StripHtmlAndEntities(instance);

            Assert.DoesNotContain("&amp;", result);
            Assert.DoesNotContain("<span>", result);
        }
    }
}