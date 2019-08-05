using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Serialization
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        internal static string Serialize(object body)
        {
            StringBuilder sb = new StringBuilder();

            using(StringWriter tw = new StringWriter(sb))
            {
                Serializer.Serialize(tw, body);
            }

            return sb.ToString();
        }
    }
}
