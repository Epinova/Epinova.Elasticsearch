using System;
using System.Collections;
using System.Linq;
using Epinova.ElasticSearch.Core.Utilities;
using TestData;
using Xunit;

namespace Core.Tests.Utilities
{
    public class ArrayHelperTests
    {
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(TestPage))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(char))]
        public void ToArray_NonEnumerableType_ReturnsEmptyArray(Type instance)
        {
            var result = ArrayHelper.ToArray(instance);

            Assert.Equal(Enumerable.Empty<object>(), result);
        }

        [Theory]
        [InlineData("foo")]
        [InlineData(new[] { 1, 2, 3 })]
        [InlineData(new[] { '1', '2', '3' })]
        [InlineData(new[] { Double.MaxValue, Double.MinValue, Double.MaxValue })]
        public void ToArray_EnumerableType_ReturnsArray(object instance)
        {
            var result = (IEnumerable)ArrayHelper.ToArray(instance);

            Assert.NotEmpty(result);
        }
    }
}