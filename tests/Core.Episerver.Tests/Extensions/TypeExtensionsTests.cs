using System;
using Epinova.ElasticSearch.Core.Extensions;
using EPiServer.Core;
using TestData;
using Xunit;

namespace Core.Episerver.Tests.Extensions
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class TypeExtensionsTests
    {
        [Fact]
        public void GetUnproxiedType_TypeIsNotProxyTargetAccessor_ReturnsType()
        {
            const int instance = 1;

            Type instanceType = instance.GetType();

            Type result = instance.GetUnproxiedType();

            Assert.Equal(instanceType, result);
        }

        [Fact]
        public void GetInheritancHierarchyArray_Null_ReturnsEmptyArray()
        {
            Type instance = null;

            string[] result = instance.GetInheritancHierarchyArray();

            Assert.Equal(new string[0], result);
        }

        [Fact]
        public void GetInheritancHierarchyArray_ValidType_ReturnsExpectedArray()
        {
            Type instance = typeof(TestPage);

            string[] result = instance.GetInheritancHierarchyArray();

            Assert.Contains("TestData_TestPage", result);
            Assert.Contains("EPiServer_Core_PageData", result);
            Assert.Contains("System_Object", result);
            Assert.Contains("EPiServer_Core_IContentData", result);
            Assert.Contains("EPiServer_Security_IContentSecurable", result);
            Assert.Contains("EPiServer_Core_IContent", result);
            Assert.Contains("EPiServer_Core_ILocalizable", result);
            Assert.Contains("EPiServer_Core_ILocale", result);
            Assert.Contains("EPiServer_Core_IVersionable", result);
            Assert.Contains("TestData_ITestPage", result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("a a")]
        [InlineData("aaa")]
        public void GetShortTypeName_InvalidType_ReturnsInstance(string type)
        {
            string result = type.GetShortTypeName();

            Assert.Equal(type, result);
        }

        [Theory]
        [InlineData("foo_bar", "bar")]
        [InlineData("EPiServer_Core_PageData", "PageData")]
        [InlineData("TestData_ITestPage", "ITestPage")]
        public void GetShortTypeName_ValidType_ReturnsLastSegment(string type, string expected)
        {
            string result = type.GetShortTypeName();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTypeName_NullType_ReturnsEmptyString()
        {
            Type instance = null;
            string result = instance.GetTypeName();

            Assert.Equal(String.Empty, result);
        }

        [Theory]
        [InlineData(typeof(TestPage), "TestData_TestPage")]
        [InlineData(typeof(PageData), "EPiServer_Core_PageData")]
        [InlineData(typeof(ILocale), "EPiServer_Core_ILocale")]
        public void GetTypeName_ValidType_ReturnsExpectedName(Type type, string expected)
        {
            string result = type.GetTypeName();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void IsAnonymousType_AnonymousType_ReturnsTrue()
        {
            var instance = new { Foo = 1, Bar = "2" };
            bool result = instance.GetType().IsAnonymousType();

            Assert.True(result);
        }

        [Fact]
        public void IsAnonymousType_NonAnonymousType_ReturnsFalse()
        {
            var instance = new TestPage();
            bool result = instance.GetType().IsAnonymousType();

            Assert.False(result);
        }
    }
}
