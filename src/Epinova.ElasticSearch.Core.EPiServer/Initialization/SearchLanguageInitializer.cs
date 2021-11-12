using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPiServer.Framework;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(FrameworkInitialization))]
    public class DependencyResolverInitialization : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.StructureMap().Configure(ConfigureContainer);


            context.ConfigurationComplete += (sender, args) =>
            {
                //Locate the specific implementation of IInterface we would like to eject
                InstanceRef instanceRef = args.StructureMap().Model.For<IInterface>().Instances
                    .FirstOrDefault(c => c.ReturnedType.Equals(typeof(OptimizelyImplementationOfAbstraction)));


                if(instanceRef != null)
                    //Eject it
                    args.StructureMap().Model.For<IInterface>().EjectAndRemove(instanceRef);


                args.StructureMap().Configure(ConfigureContainerAfter);
            };
        }


        private static void ConfigureContainer(ConfigurationExpression container)
        {
            //Register custom
        }


        private static void ConfigureContainerAfter(ConfigurationExpression container)
        {
            //Register our own to replace what we ejected
            container.For<IInterface>().Add<MyImplementation>();
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