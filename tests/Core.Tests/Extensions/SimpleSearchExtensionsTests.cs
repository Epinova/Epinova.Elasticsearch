using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Xunit;

namespace Core.Tests.Extensions
{
    public class SimpleSearchExtensionsTests
    {
        [Fact]
        public void AsJsonValue_Returns_Values()
        {
            SimpleQuerystringOperators value = SimpleQuerystringOperators.Not | SimpleQuerystringOperators.Or | SimpleQuerystringOperators.Phrase;
            Assert.Equal("NOT|OR|PHRASE", value.AsJsonValue());
        }

        [Fact]
        public void AsJsonValue_Returns_None_As_Single_Value()
        {
            Assert.Equal("NONE", SimpleQuerystringOperators.None.AsJsonValue());
        }

        [Fact]
        public void AsJsonValue_Returns_All_As_Single_Value()
        {
            Assert.Equal("ALL", SimpleQuerystringOperators.All.AsJsonValue());
        }
    }
}
