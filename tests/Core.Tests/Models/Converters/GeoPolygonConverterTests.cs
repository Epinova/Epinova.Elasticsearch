using System.IO;
using Epinova.ElasticSearch.Core.Models.Converters;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;
using Newtonsoft.Json;
using TestData;
using Xunit;

namespace Core.Tests.Models.Converters
{
    public class GeoPolygonConverterTests
    {
        [Fact]
        public void WriteJson_GeoPolygon_WritesExpectedOutput()
        {
            var input = new GeoPolygon("MyField",
                new[]
                {
                    new GeoPoint(59.9702837, 10.6149134),
                    new GeoPoint(59.9459601, 11.0231964),
                    new GeoPoint(59.7789455, 10.604809)
                });

            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);
            var converter = new GeoPolygonConverter();
            converter.WriteJson(jsonWriter, input, new JsonSerializer());
            stringWriter.Flush();

            var result = stringWriter.ToString();

            const string expected = "{\"geo_polygon\":{\"MyField\":{\"points\":[{\"lat\":59.9702837,\"lon\":10.6149134},{\"lat\":59.9459601,\"lon\":11.0231964},{\"lat\":59.7789455,\"lon\":10.604809}]}}}";

            Assert.Equal(expected, result);
        }

        [Fact]
        public void WriteJson_NotAGeoPolygon_WritesNothing()
        {
            var input = Factory.GetString();

            var stringWriter = new StringWriter();
            var jsonWriter = new JsonTextWriter(stringWriter);
            var converter = new GeoPolygonConverter();
            converter.WriteJson(jsonWriter, input, new JsonSerializer());
            stringWriter.Flush();

            var result = stringWriter.ToString();

            Assert.Empty(result);
        }
    }
}