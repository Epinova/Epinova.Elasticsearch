using System;
using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels.Abstractions;
using EPiServer;
using EPiServer.Core;
using EPiServer.Framework.Web.Resources;
using EPiServer.ServiceLocation;
using EPiServer.Shell.Modules;
using EPiServer.Web;

namespace Epinova.ElasticSearch.Core.EPiServer.Models.ViewModels
{
    public class BestBetsViewModel : LanguageAwareViewModelBase
    {
        public BestBetsViewModel(string currentLanguage) : base(currentLanguage)
        {
        }

        public List<ContentReference> SelectorRoots { get; set; } = new List<ContentReference> { SiteDefinition.Current.StartPage };

        public List<string> SelectorTypes { get; set; } = new List<string> { "episerver.core.pagedata" };

        public List<BestBetsByLanguage> BestBetsByLanguage { get; set; } = new List<BestBetsByLanguage>();

        public string TypeName { get; set; }
        public string SearchProviderKey { get; set; }

        public string GetEditUrlPrefix(string language) => $"{UriSupport.UIUrl}#viewsetting=viewlanguage:///{language}&context=epi.cms.contentdata:///";

        public IEnumerable<ModuleViewModel> GetModuleSettings()
        {
            // Adapted from EPiServer.Shell.UI.Bootstrapper.CreateViewModel

            // Creates a data structure that contains module resource paths (JS and CSS) and settings.
            var modules = ServiceLocator.Current.GetInstance<ModuleTable>();
            var resourceService = ServiceLocator.Current.GetInstance<IClientResourceService>();

            var moduleList = modules.GetModules()
                .Select(m => m.CreateViewModel(modules, resourceService))
                .OrderBy(mv => mv.ModuleDependencies?.Count ?? 0)
                .ToList();

            // Can be uncommented to remove a hard dependency on jQuery. ReportCenter.js is probably not needed on external pages
            // anyways, and the only thing besides it that depends on epiJQuery seems to be the ancient version of TinyMCE EPiServer
            // uses. If you want to use your own JQuery you must create a global alias called epiJQuery because the TinyMCE build is
            // hardcoded to look for it, e.g.: window.epiJQuery = jQuery;
            foreach(ModuleViewModel mvm in moduleList)
            {
                foreach(string res in mvm.ScriptResources.ToArray())
                {
                    if(res.EndsWith("/ReportCenter.js", StringComparison.OrdinalIgnoreCase))
                    {
                        mvm.ScriptResources.Remove(res);
                    }
                }
            }

            return moduleList;
        }
    }
}