using Newtonsoft.Json;

namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class PostFilter : QueryBase
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(
                this,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        }
    }
}