using System;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class Validation
    {
        internal static void EnsureNotNull(this string instance, string message = null)
        {
            if(instance == null)
            {
                throw new ArgumentNullException(message);
            }
        }

        internal static void EnsureNotNull(this object instance, string message = null)
        {
            if(instance == null)
            {
                throw new ArgumentNullException(message);
            }
        }

        private static void EnsureNotNull(this string[] instance, string message = null)
        {
            if(instance == null)
            {
                throw new ArgumentNullException(message);
            }
        }

        internal static void EnsureNotNullOrEmpty(this string[] instance, string message = null)
        {
            instance.EnsureNotNull();

            if(instance.Length == 0)
            {
                throw new ArgumentNullException(message);
            }
        }
    }
}
