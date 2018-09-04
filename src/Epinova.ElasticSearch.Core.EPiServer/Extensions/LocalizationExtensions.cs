using System;
using System.Web;
using System.Web.Mvc;
using EPiServer.Framework.Localization;

namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    public static class LocalizationExtensions
    {
        public static IHtmlString TranslateWithPathRaw(this HtmlHelper instance, string key, string localizationPath)
        {
            return instance.Raw(LocalizationService.Current.GetString(String.Concat(localizationPath, key)));
        }

        // ReSharper disable once UnusedParameter.Global
        public static string TranslateWithPath(this HtmlHelper instance, string key, string localizationPath)
        {
            return TranslateWithPath(key, localizationPath);
        }

        internal static string TranslateWithPath(string key, string localizationPath)
        {
            return LocalizationService.Current.GetString(String.Concat(localizationPath, key));
        }
    }
}