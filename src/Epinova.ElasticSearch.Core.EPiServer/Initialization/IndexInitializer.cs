using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using System.Web.Routing;
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using IndexingConvention = Epinova.ElasticSearch.Core.Conventions.Indexing;
using InitializationModule = EPiServer.Web.InitializationModule;

namespace Epinova.ElasticSearch.Core.EPiServer.Initialization
{
    [InitializableModule]
    [ModuleDependency(typeof(InitializationModule))]
    public class IndexInitializer : IInitializableModule
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(IndexInitializer));
        
        /// <summary>
        /// Assemblies to ignore when scanning for types to exclude.
        /// The names will be compared via String.StartsWith(OrdinalIgnoreCase)
        /// </summary>
      
        public void Initialize(InitializationEngine context)
        {
            try
            {
                RouteTable.Routes.MapRoute("ElasticSearchAdmin", "ElasticSearchAdmin/{controller}/{action}", new { controller = "ElasticAdmin", action = "Index" }, new { controller = GetControllers() });

                var serverinfo = context.Locate.Advanced.GetInstance<IServerInfoService>();

                _logger.Information($"Initializing Elasticsearch.\n" +
                    $"{serverinfo.GetInfo()}\nPlugins:\n" +
                    $"{String.Join("\n", serverinfo.ListPlugins().Select(p => p.ToString()))}");

                GetExcludedTypes()
                    .ForEach(type => IndexingConvention.Instance.ExcludeType(type));

                GetFileExtensions()
                    .ToList()
                    .ForEach(ext => IndexingConvention.Instance.IncludeFileType(ext));
            }
            catch(Exception ex)
            {
                // Swallow exception and fail silently. This init module shouldn't crash the site 
                // because an index is missing or the like.
                _logger.Error("Error occured while initializing module.", ex);
            }
        }

        public void Uninitialize(InitializationEngine context)
        {
            // Not applicable
        }

        private static string GetControllers()
        {
            var cmsAssembly = Assembly.GetExecutingAssembly();
            List<string> controllers = GetControllers(cmsAssembly).ToList();

            bool commerceEnabled = ServiceLocator.Current.GetInstance<IElasticSearchSettings>().CommerceEnabled;
            if(commerceEnabled)
            {
                Assembly commerceAssembly = Assembly.Load("Epinova.ElasticSearch.Core.EPiServer.Commerce");
                var commerceControllers = GetControllers(commerceAssembly).ToList();
                
                if(commerceControllers.Any())
                    controllers.AddRange(commerceControllers);
            }

            return String.Join("|", controllers);
        }

        private static IEnumerable<string> GetControllers(Assembly assembly)
        {
            return assembly?
                       .GetTypes()
                       .Where(type => typeof(Controller).IsAssignableFrom(type))
                       .Select(c => c.Name.Substring(0, c.Name.IndexOf("Controller", StringComparison.OrdinalIgnoreCase)))
                   ?? Enumerable.Empty<string>();
        }

        private static IEnumerable<string> GetFileExtensions()
        {
            ElasticSearchSection config = ElasticSearchSection.GetConfiguration();

            if(!config.Files.Enabled)
            {
                _logger.Information("File indexing disabled");
                yield break;
            }

            _logger.Information($"Adding allowed file extensions from config. Max size is {config.Files.ParsedMaxsize}");

            foreach(FileConfiguration file in config.Files)
            {
                _logger.Information($"Found: {file.Extension}");
                yield return file.Extension;
            }
        }

        private static List<Type> GetExcludedTypes()
        {
            var types = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName).ToList();

            AssemblySettings.IgnoreList.ToList().ForEach(
                b => assemblies.RemoveAll(a => a.FullName.StartsWith(b, StringComparison.OrdinalIgnoreCase)));

            foreach(var assembly in assemblies)
            {
                try
                {
                    types.AddRange(
                        assembly.GetTypes()
                            .Where(t => t.GetCustomAttributes(typeof(ExcludeFromSearchAttribute)).Any()));
                }
                catch(ReflectionTypeLoadException ex)
                {
                    _logger.Error($"Error while scanning assembly '{assembly.FullName}'");
                    ex.LoaderExceptions.ToList().ForEach(e => _logger.Error("LoaderException", e));
                }
                catch
                {
                    _logger.Error($"Error while scanning assembly '{assembly.FullName}'");
                }
            }

            return types;
        }
    }
}