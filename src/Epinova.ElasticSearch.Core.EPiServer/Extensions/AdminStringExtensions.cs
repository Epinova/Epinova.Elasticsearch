namespace Epinova.ElasticSearch.Core.EPiServer.Extensions
{
    public static class AdminStringExtensions
    {
        public static string FixInput(this string input)
        {
            return input.Replace("\"", "\'\'");
        }
    }
}
