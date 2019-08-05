using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Mvc;
using EPiServer;

namespace Epinova.ElasticSearch.Core.Extensions
{
    public static class UrlExtensions
    {
        public static string FacetFilterUrl(this UrlHelper instance, string facetName, object facetValue, bool replace = false, bool removeAllOtherFacets = false)
        {
            var ub = new UrlBuilder(HttpContext.Current.Request.Url);
            string facet = ub.QueryCollection[facetName];
            ub.QueryCollection.Remove(facetName);

            if(removeAllOtherFacets)
            {
                ub.QueryCollection = new NameValueCollection();
            }

            if(facetValue != null)
            {
                ub.QueryCollection.Add(facetName, replace || String.IsNullOrEmpty(facet) ? facetValue.ToString() : String.Concat(facet, "¤", facetValue));
            }

            return ub.ToString();
        }
    }
}