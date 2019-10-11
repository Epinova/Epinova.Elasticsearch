namespace Epinova.ElasticSearch.Core.Models.Query
{
    internal class ScriptSort : Sort
    {
        public string Type { get; set; }

        public string Script { get; set; }

        public string Language { get; set; }

        public object Parameters { get; set; }
    }
}