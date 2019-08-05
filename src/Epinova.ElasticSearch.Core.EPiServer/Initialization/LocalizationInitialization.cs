using System;
using System.IO;
using System.Linq;
using System.Reflection;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Framework.Localization;
using EPiServer.Framework.Localization.XmlResources;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [ModuleDependency(typeof(FrameworkInitialization))]
    public class LocalizationInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            if(context.Locate.Advanced.GetInstance<LocalizationService>() is ProviderBasedLocalizationService localizationService)
            {
                var ass = Assembly.GetAssembly(typeof(LocalizationInitialization));

                string[] xmlResources =
                    ass.GetManifestResourceNames()
                        .Where(r => r.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase))
                        .ToArray();

                foreach(string name in xmlResources)
                {
                    Stream stream = ass.GetManifestResourceStream(name);
                    var provider = new XmlLocalizationProvider();
                    provider.Initialize(name, null);
                    provider.Load(stream);
                    localizationService.AddProvider(provider);
                }
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Not applicable
        }
    }
}
