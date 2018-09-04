using System;
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
        [InlineData(Int64.MaxValue)]
        [InlineData(Int64.MinValue)]
        public void IsNumeric_Number_ReturnsTrue(string instance)
        {
            bool result = TextUtil.IsNumeric(instance);

            Assert.True(result);
        }
    }
}