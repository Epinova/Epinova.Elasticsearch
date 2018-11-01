using System.Net.Http;
using Epinova.ElasticSearch.Core.Conventions;
using TestData;
using Xunit;

namespace Core.Tests.Conventions
{
    public class MessageHandlerTests
    {
        public MessageHandlerTests()
        {
            Factory.SetupServiceLocator();
        }

        [Fact]
        public void ExcludeType_AnyType_AddsToCollection()
        {
            MessageHandlers.Instance.IncludeMessageHandler(new MockHandler());

            HttpMessageHandler result = MessageHandlers.Handler;

            Assert.IsType<MockHandler>(result);
        }

        private class MockHandler : DelegatingHandler
        {

        }
    }
}