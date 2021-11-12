using System.Globalization;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface ISearchLanguage
    {
        CultureInfo SearchLanguage { get; }
    }
}
