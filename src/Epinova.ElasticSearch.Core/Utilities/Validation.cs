using System;
using System.Linq;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Validation
    {
        internal static void EnsureNotNull(this string instance, string message = null)
        {
            if (instance == null)
                throw new NullReferenceException(message);
        }

        internal static void EnsureNotNull(this object instance, string message = null)
        {
            if (instance == null)
                throw new NullReferenceException(message);
        }

        private static void EnsureNotNull(this string[] instance, string message = null)
        {
            if (instance == null)
                throw new NullReferenceException(message);
        }

        internal static void EnsureNotNullOrEmpty(this string[] instance, string message = null)
        {
            instance.EnsureNotNull();

            if (!instance.Any())
                throw new ArgumentException(message);
        }
    }
}
