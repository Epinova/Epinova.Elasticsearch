using System;
using System.Web;
using System.Web.Mvc;
using EPiServer.Framework.Localization;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    public static class LocalizationExtensions
    {
        public static IHtmlString TranslateWithPathRaw(this HtmlHelper instance, string key, string localizationPath)
            => instance.Raw(LocalizationService.Current.GetString(String.Concat(localizationPath, key)));

        public static string TranslateWithPath(this HtmlHelper instance, string key, string localizationPath)
            => TranslateWithPath(key, localizationPath);

        internal static string TranslateWithPath(string key, string localizationPath)
            => LocalizationService.Current.GetString(String.Concat(localizationPath, key));
    }
}