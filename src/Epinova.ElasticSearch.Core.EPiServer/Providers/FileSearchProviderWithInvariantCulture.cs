using System.Globalization;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Shell.Search;

namespace Epinova.ElasticSearch.Core.EPiServer.Providers
{
    [SearchProvider]
    public class FileSearchProviderWithInvariantCulture : SearchProviderBase<MediaData, MediaData, ContentType>
    {
        public FileSearchProviderWithInvariantCulture() : base("file")
        {
            IconClass = ProviderConstants.FileIconCssClass;
            AreaName = ProviderConstants.FileArea;
        }

        protected override CultureInfo GetLanguage() => CultureInfo.InvariantCulture;
    }
}