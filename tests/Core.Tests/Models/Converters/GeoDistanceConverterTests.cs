using System.IO;
using Epinova.ElasticSearch.Core.Models.Converters;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;
using TestData;
using Xunit;

namespace Core.Tests.Models.Converters
{
    public class GeoDistanceConverterTests
    {
        [Fact]
        public void WriteJson_GeoDistance_WritesExpectedOutput()
        {
            var input = new GeoDistance(
                "MyField",
                "123km",
                new GeoPoint(59.8881646, 10.7983952));

            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);
            var converter = new GeoDistanceConverter();
            converter.WriteJson(jsonWriter, input, new JsonSerializer());
            stringWriter.Flush();

            var result = stringWriter.ToString();

            const string expected = "{\"geo_distance\":{\"distance\":\"123km\",\"MyField\":{\"lat\":59.8881646,\"lon\":10.7983952}}}";

            Assert.Equal(expected, result);
        }

        [Fact]
        public void WriteJson_NotAGeoDistance_WritesNothing()
        {
            var input = Factory.GetString();

            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);
            var converter = new GeoDistanceConverter();
            converter.WriteJson(jsonWriter, input, new JsonSerializer());
            stringWriter.Flush();

            var result = stringWriter.ToString();

            Assert.Empty(result);
        }
    }
}