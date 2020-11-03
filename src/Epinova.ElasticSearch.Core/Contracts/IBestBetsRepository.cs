using System;
using System.Collections.Generic;
using Epinova.ElasticSearch.Core.Conventions;
using EPiServer.Core;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IBestBetsRepository
    {
        void AddBestBet(string languageId, string phrase, ContentReference contentLink, string index, Type type);
        void DeleteBestBet(string languageId, string phrase, string id, string index, Type type);
        IEnumerable<BestBet> GetBestBets(string languageId, string index);
        IEnumerable<string> GetBestBetsForContent(string languageId, int contentId, string index, bool isCommerceContent = false);
    }
}