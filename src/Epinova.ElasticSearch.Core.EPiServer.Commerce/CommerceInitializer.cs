using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Commerce
{
    [InitializableModule]
    [ModuleDependency(typeof(Initialization.IndexInitializer))]
    public class CommerceInitializer : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            settings.CommerceEnabled = true;
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }
    }
}