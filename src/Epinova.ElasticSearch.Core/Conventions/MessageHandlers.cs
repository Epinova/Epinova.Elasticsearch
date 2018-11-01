
using System.Net.Http;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public class MessageHandlers
    {
        public static MessageHandlers Instance { get; } = new MessageHandlers();

        internal static DelegatingHandler Handler;

        public MessageHandlers IncludeMessageHandler(DelegatingHandler handler)
        {
            Handler = handler;
            return this;
        }
    }
}