using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class BestBetsByLanguage : LanguageViewModelBase
    {
        public IEnumerable<BestBet> BestBets { get; set; }
    }
}
