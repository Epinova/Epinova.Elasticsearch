using Epinova.ElasticSearch.Core.Models.Properties;
using Xunit;

namespace Core.Tests.Models.Properties
{
    public class GeoPointTests
    {
        [Theory]
        [InlineData("asd")]
        [InlineData(" ")]
        [InlineData(null)]
        [InlineData("-123.456,-234.567")]
        public void Parse_InvalidInput_ReturnsNull(string input)
        {
            var result = GeoPoint.Parse(input);
            Assert.Null(result);
        }

        [Theory]
        [InlineData("10,10")]
        [InlineData("-10,-10")]
        [InlineData("59.9152868,10.7519013")]
        [InlineData("-34.6158037,-58.5033379")]
        [InlineData("35.6681625,139.6007856")]
        public void Parse_ValidInput_ReturnsInstance(string input)
        {
            var result = GeoPoint.Parse(input);
            Assert.NotNull(result);
        }

        [Fact]
        public void ToString_UsesInvariantCommaSeparator()
        {
            var coords = "35.6681625,139.6007856";
            var instance = GeoPoint.Parse(coords);
            var result = instance.ToString();
            Assert.Equal(coords, result);
        }
    }
}