using Epinova.ElasticSearch.Core;

namespace TestData
{
    public static class ElasticFixtureSettings
    {
        public static readonly string IndexName = $"my-index{Constants.IndexNameLanguageSplitChar}no";
        public static string IndexNameWithoutLang = "my-index";
    }
}