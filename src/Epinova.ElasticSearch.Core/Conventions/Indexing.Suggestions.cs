using System.Collections.Generic;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        private static readonly List<Suggestion> SuggestionList;

        internal static List<Suggestion> Suggestions => SuggestionList;
    }
}