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
        public void SetMessageHandler_IsCorrectType()
        {
            MessageHandler.Instance.SetMessageHandler(new MockHandler());

            HttpMessageHandler result = MessageHandler.Handler;

            Assert.IsType<MockHandler>(result);
        }

        private class MockHandler : DelegatingHandler
        {

        }
    }
}