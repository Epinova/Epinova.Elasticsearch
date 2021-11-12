using Epinova.ElasticSearch.Core.Contracts;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(FrameworkInitialization))]
    public class DependencyResolverInitialization : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.ConfigurationComplete += (sender, args) =>
            {
                context.Services.RemoveAll<ISearchLanguage>();
                context.Services.Add<ISearchLanguage, EpiserverSearchLanguage>(ServiceInstanceScope.Hybrid);
            };
        }

        public void Initialize(InitializationEngine context)
        {
        }
        
        public void Uninitialize(InitializationEngine context)
        {
        }


        public void Preload(string[] parameters)
        {
        }
    }
}