using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using EPiServer.Core;
using EPiServer.Core.Html.StringParsing;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    internal static class XhtmlStringExtensions
    {
        public static IEnumerable<ContentFragment> GetFragments(this XhtmlString instance, IPrincipal principal = null)
        {
            return principal == null
                ? instance.Fragments.OfType<ContentFragment>()
                : instance.Fragments.GetFilteredFragments(principal).OfType<ContentFragment>();
        }
    }
}