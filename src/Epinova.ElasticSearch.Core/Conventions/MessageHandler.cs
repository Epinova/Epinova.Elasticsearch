using System.Net.Http;

namespace Epinova.ElasticSearch.Core.Conventions
{
    /// <summary>
    /// This implementation contains a single instance of <see cref="HttpMessageHandler" />.
    /// If you want several <see cref="HttpMessageHandler" />s, we recommend chaining them before including.
    /// </summary>
    public sealed class MessageHandler
    {
        private MessageHandler()
        {
        }

        public static readonly MessageHandler Instance = new MessageHandler();

        internal HttpMessageHandler Handler;

        public void SetMessageHandler(HttpMessageHandler handler)
        {
            Handler = handler;
        }
    }
}