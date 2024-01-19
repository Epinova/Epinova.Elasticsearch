using System;
using System.Linq;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed class BestBet
    {
        public BestBet(string phrase, long id)
        {
            Id = id;
            Phrase = phrase;
        }

        public long Id { get; }

        public string Provider => "";
        public string Url => "";

        public string Name { get; set; }

        public string Phrase { get; }

        internal string[] GetTerms()
        {
            return String.IsNullOrWhiteSpace(Phrase)
                ? Array.Empty<string>()
                : Phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToArray();
        }
    }
}