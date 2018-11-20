using EPiServer.Shell;
using EPiServer.Shell.ViewComposition;

namespace Epinova.ElasticSearch.Core.EPiServer.Components.MoreLikeThis
{
    /// <summary>
    /// Used to perform a MLT query, ref. https://www.elastic.co/guide/en/elasticsearch/reference/current/query-dsl-mlt-query.html
    /// Inspired by https://world.episerver.com/blogs/Linus-Ekstrom/Dates/2012/11/Creating-a-component-that-searches-for-content/
    /// </summary>
    [Component]
    public class MoreLikeThisComponent : ComponentDefinitionBase
    {
        public MoreLikeThisComponent() : base("epinova-elasticsearch/MoreLikeThisSearch")
        {
            PlugInAreas = new[] { PlugInArea.Assets, "/episerver/commerce/assets" };
            Categories = new[] { "commerce", "cms" };
            LanguagePath = "/epinovaelasticsearch/components/mlt";
        }
    }
}
