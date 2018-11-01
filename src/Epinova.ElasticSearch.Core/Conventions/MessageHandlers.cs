using System.Net.Http;

namespace Epinova.ElasticSearch.Core.Conventions
{
    /// <summary>
    /// This implementation contains a single instance of <see cref="HttpMessageHandler" />.
    /// If you want several <see cref="HttpMessageHandler" />s, we recommend chaining them before including.
    /// </summary>
    public class MessageHandlers
    {
        public static MessageHandlers Instance = new MessageHandlers();

        internal static HttpMessageHandler Handler;

        public MessageHandlers IncludeMessageHandler(HttpMessageHandler handler)
        {
            Handler = handler;
            return this;
        }
    }
}