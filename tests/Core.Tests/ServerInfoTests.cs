using System;
using System.Linq;
using Epinova.ElasticSearch.Core;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Settings;
using Moq;
using Xunit;

namespace Core.Tests
{
    public class ServerInfoTests
    {
        private readonly ServerInfoService _service;
        private readonly Mock<IHttpClientHelper> _clientMock;

        public ServerInfoTests()
        {
            var settingsMock = new Mock<IElasticSearchSettings>();
            settingsMock.Setup(m => m.Host).Returns("http://example.com");

            _clientMock = new Mock<IHttpClientHelper>();

            _service = new ServerInfoService(_clientMock.Object, settingsMock.Object);
        }

        [Fact]
        public void ListPlugins_ReturnsCorrectData()
        {
            _clientMock
                .Setup(m => m.GetString(It.IsAny<Uri>()))
                .Returns("[{\"component\":\"ingest-attachment\",\"version\":\"6.8.6\"}]");

            var result = _service.ListPlugins().First();

            Assert.Equal("ingest-attachment", result.Component);
            Assert.Equal(new Version(6, 8, 6), result.Version);
        }

        [Fact]
        public void GetInfo_ReturnsCorrectData()
        {
            _clientMock
                .Setup(m => m.GetString(It.IsAny<Uri>()))
                .Returns(@"{
                      ""name"" : ""myserver"",
                      ""cluster_name"" : ""elasticsearch"",
                      ""cluster_uuid"" : ""06DFv3nvR82ISXsi1EMYiQ"",
                      ""version"" : {
                        ""number"" : ""6.8.6"",
                        ""build_flavor"" : ""default"",
                        ""build_type"" : ""zip"",
                        ""build_hash"" : ""3d9f765"",
                        ""build_date"" : ""2019-12-13T17:11:52.013738Z"",
                        ""build_snapshot"" : false,
                        ""lucene_version"" : ""7.7.2"",
                        ""minimum_wire_compatibility_version"" : ""5.6.0"",
                        ""minimum_index_compatibility_version"" : ""5.0.0""
                      },
                      ""tagline"" : ""You Know, for Search""
                    }");

            var result = _service.GetInfo();

            Assert.Equal(new Version(6, 8, 6), result.Version);
            Assert.Equal("myserver", result.Name);
        }
    }
}
