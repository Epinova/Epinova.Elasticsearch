namespace Epinova.ElasticSearch.Core.Models
{
    public class RawResults<T>
    {
        public T RootObject { get; set; }

        public string RawJson { get; set; }
    }
}