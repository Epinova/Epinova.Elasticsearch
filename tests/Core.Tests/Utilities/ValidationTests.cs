using System;
using Epinova.ElasticSearch.Core.Utilities;
using Xunit;

namespace Core.Tests.Utilities
{
    public class ValidationTests
    {
        [Fact]
        public void EnsureNotNull_NullValue_Throws()
        {
            const string instance = null;

            Assert.Throws<ArgumentNullException>(() => instance.EnsureNotNull());
        }

        [Fact]
        public void EnsureNotNull_ValidString_DoesNotThrow()
        {
            const string instance = "a";

            instance.EnsureNotNull();
        }

        [Fact]
        public void EnsureNotNullOrEmpty_NullValue_Throws()
        {
            string[] instance = null;

            Assert.Throws<ArgumentNullException>(() => instance.EnsureNotNullOrEmpty());
        }

        [Fact]
        public void EnsureNotNullOrEmpty_EmptyArray_Throws()
        {
            var instance = new string[0];

            Assert.Throws<ArgumentNullException>(() => instance.EnsureNotNullOrEmpty());
        }

        [Fact]
        public void EnsureNotNullOrEmpty_ValidArray_DoesNotThrow()
        {
            string[] instance = { "a" };

            instance.EnsureNotNullOrEmpty();
        }
    }
}