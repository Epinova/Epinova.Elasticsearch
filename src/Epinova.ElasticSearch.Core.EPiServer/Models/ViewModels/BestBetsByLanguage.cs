using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Conventions;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class BestBetsByLanguage
    {
        public string LanguageId { get; set; }

        public string LanguageName { get; set; }

        public IEnumerable<BestBet> BestBets { get; set; }
    }
}
