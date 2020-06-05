using System;
using EPiServer.ServiceLocation;

namespace TestData
{
    public sealed class ServiceLocatorFixture : IDisposable
    {
        public ServiceLocatorFixture()
        {
            ServiceLocationMock = Factory.SetupServiceLocator();
        }

        public void Dispose() => ServiceLocator.SetLocator(null);

        public ServiceLocationMock ServiceLocationMock { get; }

        public void MockInfoEndpoints()
        {
            ServiceLocationMock.HttpClientMock
                .Setup(m => m.GetJson(new Uri("http://example.com/_cat/indices?format=json")))
                .Returns(Factory.GetJsonTestData("IndicesInfo.json"));
            ServiceLocationMock.HttpClientMock
                .Setup(m => m.GetJson(new Uri("http://example.com/_cat/health?format=json")))
                .Returns(Factory.GetJsonTestData("HealthInfo.json"));
            ServiceLocationMock.HttpClientMock
                .Setup(m => m.GetJson(new Uri("http://example.com/_cat/nodes?format=json&h=m,v,http,d,rc,rm,u,n")))
                .Returns(Factory.GetJsonTestData("NodeInfo.json"));
            ServiceLocationMock.HttpClientMock
                .Setup(m => m.GetString(new Uri("http://example.com/my-index-no/_settings")))
                .Returns(Factory.GetJsonTestData("Settings.json"));
        }
    }
}
