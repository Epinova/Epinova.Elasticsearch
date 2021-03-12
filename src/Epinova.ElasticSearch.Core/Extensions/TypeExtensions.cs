using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using EPiServer;
using EPiServer.Core;
using EPiServer.Data.Entity;
using EPiServer.DataAnnotations;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Extensions
{
    internal static class TypeExtensions
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(TypeExtensions));

        public static string[] GetInheritancHierarchyArray(this Type type)
        {
            if(type == null)
            {
                return new string[0];
            }

            return type.GetInheritancHierarchy().Select(GetTypeName).ToArray();
        }

        public static string GetShortTypeName(this string typeName)
        {
            if(String.IsNullOrWhiteSpace(typeName) || typeName.IndexOf("_", StringComparison.OrdinalIgnoreCase) == -1)
            {
                return typeName;
            }

            return typeName.Split('_').Last();
        }

        /// <summary>
        /// Get the fully qualified type name
        /// </summary>
        /// <returns>The full namespace of the supplied type, with underscores instead of dots</returns>
        public static string GetTypeName(this Type type)
        {
            if(type?.FullName == null)
            {
                return String.Empty;
            }

            if(type.IsAnonymousType())
            {
                return "AnonymousType";
            }

            return type.FullName?.Replace(".", "_");
        }

        internal static bool IsExcludedType(this Type type)
        {
            if(type?.Namespace is null)
            {
                return false;
            }

            if(type.Namespace.StartsWith("Epinova.ElasticSearch", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return Indexing.ExcludedTypes.Contains(type)
                   || type.GetCustomAttributes(typeof(ExcludeFromSearchAttribute), true).Length > 0
                   || DerivesFromExcludedType(type);
        }

        private static bool DerivesFromExcludedType(Type typeToCheck)
        {
            return Indexing.ExcludedTypes
                .Any(type => (type.IsClass && typeToCheck.IsSubclassOf(type))
                             || (type.IsInterface && type.IsAssignableFrom(typeToCheck)));
        }


        internal static Type GetUnproxiedType(this object source)
        {
            return source.GetOriginalType();
        }

        internal static bool IsAnonymousType(this Type type)
        {
            return type?.FullName != null
                   && type.FullName.StartsWith("<>")
                   && type.FullName.Contains("AnonymousType");
        }

        internal static Type GetTypeFromTypeCode(this Type type)
        {
            TypeCode typeCode = Type.GetTypeCode(type.GetElementType());
            return Type.GetType("System." + typeCode);
        }

        internal static List<PropertyInfo> GetIndexableProps(this Type contentType, bool optIn)
        {
            return contentType.GetProperties()
                .Where(prop => IsIndexable(contentType, prop, optIn))
                .ToList();
        }

        internal static IEnumerable<Type> GetInheritancHierarchy(this Type type)
        {
            for(var current = type; current != null; current = current.BaseType)
            {
                yield return current;
            }

            if(type == null)
            {
                yield break;
            }

            IEnumerable<Type> interfaces = type
                .GetInterfaces()
                .Cast<TypeInfo>()
                .Where(i => !i.ImplementedInterfaces.Contains(typeof(IReadOnly)));

            foreach(var i in interfaces)
            {
                yield return i;
            }
        }

        private static bool IsIndexable(Type contentType, PropertyInfo p, bool optIn)
        {
            if(p == null || contentType == null)
            {
                return false;
            }

            Logger.Debug("IsIndexable: " + contentType.Name + " -> " + p.Name);

            if(typeof(BlockData).IsAssignableFrom(p.PropertyType))
            {
                Logger.Debug("Maybe: Local block");
                return true;
            }

            if(WellKnownProperties.Ignore.Contains(p.Name))
            {
                Logger.Debug("No: WellKnownProperties.Ignore");
                return false;
            }

            var explicitIncludes = Indexing.Instance.SearchableProperties;
            if(explicitIncludes.ContainsKey(contentType) && explicitIncludes[contentType].Contains(p.Name))
            {
                Logger.Debug("Yes: Explicit");
                return true;
            }

            var searchable = p.GetCustomAttribute<SearchableAttribute>();
            if(searchable != null)
            {
                Logger.Debug(searchable.IsSearchable
                    ? "Yes: Attribute"
                    : "No: Attribute");

                return searchable.IsSearchable;
            }

            if(optIn)
            {
                return false;
            }

            if(p.PropertyType == typeof(ContentArea))
            {
                Logger.Debug("No: Unmarked ContentArea");
                return false;
            }

            if(p.PropertyType == typeof(string) || p.PropertyType == typeof(XhtmlString))
            {
                Logger.Debug("Yes: Implicit string");
                return true;
            }

            if(p.PropertyType.GetInterfaces().Contains(typeof(IProperty)))
            {
                Logger.Debug("Yes: IProperty implementation");
                return true;
            }

            Logger.Debug("Nope");
            return false;
        }
    }
}