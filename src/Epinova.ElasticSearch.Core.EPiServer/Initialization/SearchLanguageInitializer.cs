//using Epinova.ElasticSearch.Core.Contracts;
//using EPiServer.Framework;
//using EPiServer.Framework.Initialization;
//using EPiServer.ServiceLocation;
//using StructureMap;

//namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
//{
//    [InitializableModule]
//    [ModuleDependency(typeof(FrameworkInitialization))]
//    public class DependencyResolverInitialization : IConfigurableModule
//    {
//        public void ConfigureContainer(ServiceConfigurationContext context)
//        {
//            context.StructureMap().Configure(ConfigureContainer);


//            context.ConfigurationComplete += (sender, args) =>
//            {
//                args.StructureMap().Model.For<ISearchLanguage>().EjectAndRemove();


//                args.StructureMap().Configure(ConfigureContainerAfter);
//            };
//        }


//        private static void ConfigureContainer(ConfigurationExpression container)
//        {
//            //Register custom
//        }


//        private static void ConfigureContainerAfter(ConfigurationExpression container)
//        {
//            //Register our own to replace what we ejected
//            container.For<ISearchLanguage>().Add<EpiserverSearchLanguage>();
//        }


//        public void Initialize(InitializationEngine context)
//        {
//        }


//        public void Uninitialize(InitializationEngine context)
//        {
//        }

//        public void Preload(string[] parameters)
//        {
//        }
//    }
//}