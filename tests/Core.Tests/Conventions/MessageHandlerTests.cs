using System.Net.Http;
using Epinova.ElasticSearch.Core.Conventions;
using Xunit;

namespace Core.Tests.Conventions
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class MessageHandlerTests
    {
        [Fact]
        public void SetMessageHandler_IsCorrectType()
        {
            MessageHandler.Instance.SetMessageHandler(new MockHandler());

            HttpMessageHandler result = MessageHandler.Instance.Handler;

            Assert.IsType<MockHandler>(result);
        }

        private class MockHandler : DelegatingHandler
        {
        }
    }
}