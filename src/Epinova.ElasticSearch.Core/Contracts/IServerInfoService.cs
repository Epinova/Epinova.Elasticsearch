using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Models.Admin;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IServerInfoService
    {
        ServerInfo GetInfo();
        IEnumerable<Plugin> ListPlugins();
    }
}