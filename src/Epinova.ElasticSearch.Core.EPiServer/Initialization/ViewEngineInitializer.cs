using System.Web;
using System.Web.Mvc;
using System.Web.WebPages;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Initialization.Internal;
using RazorGenerator.Mvc;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(PlugInInitialization))]
    public class ViewEngineInitializer : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var engine = new PrecompiledMvcEngine(typeof(ViewEngineInitializer).Assembly)
            {
                UsePhysicalViewsIfNewer = HttpContext.Current.Request.IsLocal
            };

            ViewEngines.Engines.Insert(0, engine);

            // StartPage lookups are done by WebPages. 
            VirtualPathFactoryManager.RegisterVirtualPathFactory(engine);
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Not applicable
        }
    }
}