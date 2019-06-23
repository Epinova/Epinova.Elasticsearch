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
    }
}
