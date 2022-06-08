using System.Globalization;
using Epinova.ElasticSearch.Core.Contracts;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core
{
    [ServiceConfiguration(typeof(ISearchLanguage), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class DefaultSearchLanguage : ISearchLanguage
    {
        public CultureInfo SearchLanguage => CultureInfo.CurrentCulture;
    }
}
