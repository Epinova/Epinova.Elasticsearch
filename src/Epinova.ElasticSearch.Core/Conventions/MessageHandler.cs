using System.Net.Http;

namespace Epinova.ElasticSearch.Core.Conventions
{
    /// <summary>
    /// This implementation contains a single instance of <see cref="HttpMessageHandler" />.
    /// If you want several <see cref="HttpMessageHandler" />s, we recommend chaining them before including.
    /// </summary>
    public class MessageHandler
    {
        public static MessageHandler Instance = new MessageHandler();

        internal static HttpMessageHandler Handler;

        public MessageHandler SetMessageHandler(HttpMessageHandler handler)
        {
            Handler = handler;
            return this;
        }
    }
}