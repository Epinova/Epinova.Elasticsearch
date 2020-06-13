using System;
using Epinova.ElasticSearch.Core.Admin;
using Xunit;

namespace Core.Tests.Admin
{
    public class IndexTests
    {
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData(" ")]
        public void Ctor_NullOrEmptyIndexName_Throws(string indexName)
            => Assert.Throws<InvalidOperationException>(() => new Index(null, null, null, indexName));
    }
}