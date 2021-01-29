using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

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
    }
}