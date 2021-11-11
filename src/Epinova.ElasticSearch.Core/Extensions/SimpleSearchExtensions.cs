using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Epinova.ElasticSearch.Core.Enums;

namespace Epinova.ElasticSearch.Core.Extensions
{
    public static class SimpleSearchExtensions
    {
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

            return string.Join("|", Enum.GetValues(typeof(SimpleQuerystringOperators))
                .Cast<SimpleQuerystringOperators>()
                .Where(x => instance.HasFlag(x) && Convert.ToInt32(x) > 0)
                .DefaultIfEmpty()
                .Select(f => typeof(SimpleQuerystringOperators).GetField(f.ToString()).GetCustomAttribute<EnumMemberAttribute>()?.Value ?? f.ToString()));
        }
    }
}