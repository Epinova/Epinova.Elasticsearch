using System;
using System.Collections.Generic;
using System.Globalization;
using Epinova.ElasticSearch.Core.Conventions;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IBestBetsRepository
    {
        void AddBestBet(CultureInfo language, string phrase, long id, string index, Type type);
        void DeleteBestBet(CultureInfo language, string phrase, long id, string index, Type type);
        IEnumerable<BestBet> GetBestBets(CultureInfo language, string index);
        IEnumerable<string> GetBestBetsForContent(CultureInfo language, int contentId, string index, bool isCommerceContent = false);
    }
}