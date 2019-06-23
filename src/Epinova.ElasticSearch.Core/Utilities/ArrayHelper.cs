using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class ArrayHelper
    {
        internal static object ToArray(object value)
        {
            var enumerable = value as IEnumerable;
            if(enumerable == null)
            {
                return Enumerable.Empty<object>();
            }

            Type type = GetIEnumerableType(value);
            if(type == null)
            {
                return Enumerable.Empty<object>();
            }

            List<object> list = enumerable.Cast<object>().ToList();
            Array values = Array.CreateInstance(type, list.Count);

            var i = 0;
            foreach(object e in list)
            {
                values.SetValue(e, i++);
            }

            return values;
        }

        internal static object ToDictionary(object value)
            => value as IDictionary<string, object>;

        internal static bool IsArrayCandidate(PropertyInfo p)
            => IsArrayCandidate(p?.PropertyType);

        internal static bool IsArrayCandidate(Type type)
        {
            return type != null && !typeof(string).IsAssignableFrom(type)
                   && typeof(IEnumerable).IsAssignableFrom(type)
                   && !IsDictionary(type);
        }

        internal static bool IsDictionary(Type type)
            => type.IsGenericType && type.GenericTypeArguments.Length == 2;

        private static Type GetIEnumerableType(object o)
        {
            return o?.GetType()
                .GetInterfaces()
                .Where(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(t => t.GetGenericArguments()[0])
                .FirstOrDefault();
        }
    }
}