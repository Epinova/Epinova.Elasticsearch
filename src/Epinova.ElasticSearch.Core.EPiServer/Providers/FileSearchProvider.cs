using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Shell.Search;

namespace Epinova.ElasticSearch.Core.EPiServer.Providers
{
    [SearchProvider]
    public class FileSearchProvider : SearchProviderBase<MediaData, MediaData, ContentType>
    {
        public FileSearchProvider() : base("file")
        {
            IconClass = ProviderConstants.FileIconCssClass;
            AreaName = ProviderConstants.FileArea;
        }
    }
}