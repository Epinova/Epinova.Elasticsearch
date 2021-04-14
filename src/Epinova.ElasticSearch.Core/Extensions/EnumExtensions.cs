using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Epinova.ElasticSearch.Core.Enums;

namespace Epinova.ElasticSearch.Core.Extensions
{
    public static class EnumExtensions
    {
        public static IEnumerable<string> AsEnumDescriptions<T>(this T instance) where T : Enum
        {
            return Enum.GetValues(typeof(T)).Cast<T>()
                .Where(x => instance.HasFlag(x) && Convert.ToInt32(x) > 0)
                .DefaultIfEmpty()
                .Select(f => typeof(T).GetField(f.ToString()).GetCustomAttribute<DescriptionAttribute>()?.Description ?? f.ToString());
        }

        /// <summary>
        /// Gets the value as elastic searchs json format for flags. (OR|AND|PREFIX)
        /// </summary>
        /// <param name="instance">Enum instance to format.</param>
        /// <returns>Value in elastic search json format.</returns>
        internal static string AsJsonValue(this SimpleQuerystringOperators instance)
        {
            if (instance == SimpleQuerystringOperators.All)
            {
                return typeof(SimpleQuerystringOperators)
                    .GetField(nameof(SimpleQuerystringOperators.All))
                    .GetCustomAttribute<EnumMemberAttribute>()?.Value ?? nameof(SimpleQuerystringOperators.All);
            }
            else
            {
                return
                    string.Join("|",
                        Enum.GetValues(typeof(SimpleQuerystringOperators)).Cast<SimpleQuerystringOperators>()
                        .Where(x => instance.HasFlag(x) && Convert.ToInt32(x) > 0)
                        .DefaultIfEmpty()
                        .Select(f => typeof(SimpleQuerystringOperators).GetField(f.ToString()).GetCustomAttribute<EnumMemberAttribute>()?.Value ?? f.ToString()));
            }
        }
    }
}