using System;
using Epinova.ElasticSearch.Core.Extensions;
using TestData;
using Xunit;

namespace Core.Tests.Extensions
{
    public class TypeExtensionsTests
    {
        [Fact]
        public void IsAnonymousType_NullType_ReturnsFalse()
        {
            Type type = null;

            bool result = type.IsAnonymousType();

            Assert.False(result);
        }

        [Fact]
        public void IsAnonymousType_AnonymousType_ReturnsTrue()
        {
            Type type = new { Foo = 1 }.GetType();

            bool result = type.IsAnonymousType();

            Assert.True(result);
        }

        [Fact]
        public void GetTypeName_NullType_ReturnsEmptyString()
        {
            Type type = null;

            string result = type.GetTypeName();

            Assert.Empty(result);
        }

        [Fact]
        public void GetTypeName_AnonymousType_ReturnsAnonymousTypeString()
        {
            Type type = new { Foo = 1 }.GetType();

            string result = type.GetTypeName();

            Assert.Equal("AnonymousType", result);
        }

        [Fact]
        public void GetTypeName_AnyType_ReturnsStringWithoutDot()
        {
            Type type = typeof(string);

            string result = type.GetTypeName();

            Assert.DoesNotContain(".", result);
        }

        [Fact]
        public void GetTypeName_AnyType_ReturnsStringWithUnderscore()
        {
            Type type = typeof(DateTime);

            string result = type.GetTypeName();

            Assert.Contains("_", result);
        }

        [Fact]
        public void GetInheritancHierarchyArray_NullType_ReturnsEmptyArray()
        {
            Type type = null;

            string[] result = type.GetInheritancHierarchyArray();

            Assert.Empty(result);
        }

        [Fact]
        public void GetInheritancHierarchyArray_TypeWithoutAncestors_ReturnsArrayWithOneItem()
        {
            Type type = typeof(object);

            string[] result = type.GetInheritancHierarchyArray();

            Assert.True(result.Length == 1);
        }

        [Fact]
        public void GetInheritancHierarchyArray_TypeWithtAncestors_ReturnsArrayWithAtLeastTwoItems()
        {
            Type type = typeof(TestClassA);

            string[] result = type.GetInheritancHierarchyArray();

            Assert.True(result.Length >= 2);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void GetShortTypeName_NullOrEmptyInstance_ReturnsInstance(string instance)
        {
            var actual = instance.GetShortTypeName();
            var expected = instance;

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetShortTypeName_InstanceWithoutUnderscore_ReturnsInstance()
        {
            const string instance = "foo";
            var actual = instance.GetShortTypeName();
            const string expected = instance;

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("Epi11demo_Models_Pages_StandardPage", "StandardPage")]
        [InlineData("Epi11demo_Models_Blocks_TeaserBlock", "TeaserBlock")]
        [InlineData("Epi11demo_Models_Pages_ArticlePage", "ArticlePage")]
        public void GetShortTypeName_Instance_ReturnsShortName(string instance, string expected)
        {
            var actual = instance.GetShortTypeName();

            Assert.Equal(expected, actual);
        }
    }
}