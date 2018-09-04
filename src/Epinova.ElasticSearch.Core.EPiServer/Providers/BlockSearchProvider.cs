using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.Shell.Search;

namespace Epinova.ElasticSearch.Core.EPiServer.Providers
{
    [SearchProvider]
    public class BlockSearchProvider : SearchProviderBase<BlockData, BlockData, BlockType>
    {
        public BlockSearchProvider() : base("block")
        {
            IconClass = ProviderConstants.BlockIconCssClass;
            AreaName = ProviderConstants.BlockArea;
        }
    }
}