using System.IO;
using Epinova.ElasticSearch.Core.Models.Converters;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;
using TestData;
using Xunit;

namespace Core.Tests.Models.Converters
{
    public class GeoBoundingBoxConverterTests
    {
        [Fact]
        public void WriteJson_GeoBoundingBox_WritesExpectedOutput()
        {
            var input = new GeoBoundingBox(
                "MyField",
                new GeoPoint(59.9277542, 10.7190847),
                new GeoPoint(59.8881646, 10.7983952));

            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);
            var converter = new GeoBoundingBoxConverter();
            converter.WriteJson(jsonWriter, input, new JsonSerializer());
            stringWriter.Flush();

            var result = stringWriter.ToString();

            const string expected = "{\"geo_bounding_box\":{\"MyField\":{\"top_left\":{\"lat\":59.9277542,\"lon\":10.7190847},\"bottom_right\":{\"lat\":59.8881646,\"lon\":10.7983952}}}}";

            Assert.Equal(expected, result);
        }

        [Fact]
        public void WriteJson_NotAGeoBoundingBox_WritesNothing()
        {
            var input = Factory.GetString();

            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);
            var converter = new GeoBoundingBoxConverter();
            converter.WriteJson(jsonWriter, input, new JsonSerializer());
            stringWriter.Flush();

            var result = stringWriter.ToString();

            Assert.Empty(result);
        }
    }
}