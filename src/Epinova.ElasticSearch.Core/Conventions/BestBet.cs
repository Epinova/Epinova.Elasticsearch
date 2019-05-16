using System;
using System.Linq;
using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed class BestBet
    {
        public BestBet(string phrase, ContentReference contentLink, string url = null)
        {
            Id = contentLink.ToString();
            Phrase = phrase;
            Url = url;
            Provider = contentLink.ProviderName;
        }

        public string Id { get; }

        public string Provider { get; }

        public string Name { get; set; }

        public string Url { get; }

        public string Phrase { get; }

        internal string[] GetTerms()
        {
            return String.IsNullOrWhiteSpace(Phrase)
                ? new string[0]
                : Phrase.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .ToArray();
        }
    }
}