using System;
using System.Text.RegularExpressions;
using EPiServer.Core.Html;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class TextUtil
    {
        private static readonly Regex Pattern = new Regex(@"&[^\s;]+;", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        internal static bool IsNumeric(string input)
        {
            return input != null && Int64.TryParse(input, out _);
        }

        internal static string StripHtmlAndEntities(string input)
        {
            if (input == null)
                return String.Empty;

            return Pattern.Replace(TextIndexer.StripHtml(input, Int32.MaxValue) ?? String.Empty, " ");
        }
    }
}