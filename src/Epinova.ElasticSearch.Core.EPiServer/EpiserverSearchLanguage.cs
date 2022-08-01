using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using EPiServer.DataAbstraction;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer
{
    [ServiceConfiguration(typeof(ISearchLanguage), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class EpiserverSearchLanguage : ISearchLanguage
    {
        private List<CultureInfo> _enabledLanguages;
        
        public EpiserverSearchLanguage(ILanguageBranchRepository languageBranchRepository)
        {
            _enabledLanguages = languageBranchRepository.ListEnabled().Select(l => l.Culture).ToList();
        }
        
        public CultureInfo SearchLanguage
        {
            get
            {
                if(CultureInfo.CurrentCulture.Equals(CultureInfo.InvariantCulture))
                    return CultureInfo.InvariantCulture;

                if(!_enabledLanguages.Any())
                    return CultureInfo.InvariantCulture;

                return GetEnabledCulture(CultureInfo.CurrentCulture) ?? GetEnabledCulture(CultureInfo.CurrentCulture.Parent) ?? GetEnabledCulture(CultureInfo.CurrentCulture.Parent.Parent) ?? CultureInfo.InvariantCulture;
            }
        }
        
        private CultureInfo GetEnabledCulture(CultureInfo cultureInfo)
        {
            return _enabledLanguages.SingleOrDefault(l => l.Equals(cultureInfo));
        }
    }
}