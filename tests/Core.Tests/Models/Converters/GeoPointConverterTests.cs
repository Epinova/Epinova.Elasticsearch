using Epinova.ElasticSearch.Core.Models.Properties;
using Newtonsoft.Json;
using Xunit;

namespace Core.Tests.Models.Converters
{
    public class GeoPointConverterTests
    {
        [Fact]
        public void ReadJson_CoordsString_Converts()
        {
            var json = "\"35.6681625,139.6007856\"";

            var result = JsonConvert.DeserializeObject<GeoPoint>(json);

            Assert.Equal(35.6681625, result.Lat);
            Assert.Equal(139.6007856, result.Lon);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("foo")]
        [InlineData("42")]
        public void ReadJson_InvalidString_ReturnsNull(string input)
        {
            var json = "\"" + input + "\"";

            var result = JsonConvert.DeserializeObject<GeoPoint>(json);

            Assert.Null(result);
        }
    }
}