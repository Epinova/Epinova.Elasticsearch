using System;
using Epinova.ElasticSearch.Core.Conventions;
using TestData;
using Xunit;

namespace Core.Tests.Conventions
{
    public class IndexingTests
    {
        public IndexingTests()
        {
            Factory.SetupServiceLocator();

            Indexing.Extensions.Clear();
        }


        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void IncludeFileType_NullOrEmptyString_DoesNotAddToCollection(string type)
        {
            Indexing.Instance.IncludeFileType(type);

            int result = Indexing.IncludedFileExtensions.Length;

            Assert.Equal(0, result);
        }

        [Fact]
        public void IncludeFileType_ValidString_AddsToCollection()
        {
            Indexing.Instance.IncludeFileType("pdf");

            int result = Indexing.IncludedFileExtensions.Length;

            Assert.True(result > 0);
        }

        [Fact]
        public void ExcludeType_AnyType_AddsToCollection()
        {
            Indexing.Instance.ExcludeType<string>();

            Type[] result = Indexing.ExcludedTypes;

            Assert.Contains(typeof(string), result);
        }

        [Fact]
        public void ExcludeRoot_AddsToCollection()
        {
            int rootId = Factory.GetInteger();
            Indexing.Instance.ExcludeRoot(rootId);

            int[] result = Indexing.ExcludedRoots;

            Assert.Contains(rootId, result);
        }
    }
}
