using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Models.Serialization;

namespace Epinova.ElasticSearch.Core.Models
{
    public abstract class SearchResultBase<T>
    {
        protected SearchResultBase()
        {
            Facets = new FacetEntry[0];
            Hits = Enumerable.Empty<T>();
        }

        /// <summary>
        /// The hits returned by the query
        /// </summary>
        public IEnumerable<T> Hits { get; set; }

        /// <summary>
        /// Query-time in milliseconds
        /// </summary>
        public int Took { get; set; }

        /// <summary>
        /// Total number of hits for query
        /// </summary>
        public int TotalHits { get; set; }

        /// <summary>
        /// Facets returned by the query
        /// </summary>
        public FacetEntry[] Facets { get; set; }

        /// <summary>
        /// Search phrase suggestions
        /// </summary>
        public IEnumerable<string> DidYouMean
        {
            get
            {
                if(DidYouMeanSuggestions == null || DidYouMeanSuggestions.Length == 0)
                {
                    return new string[0];
                }

                return DidYouMeanSuggestions.Select(s => s.Text);
            }
        }

        /// <summary>
        /// All search phrase suggestion
        /// </summary>
        internal DidYouMeanOption[] DidYouMeanSuggestions { get; set; }

        internal string Query { get; set; }

        internal string RawJsonOutput { get; set; }
    }
}