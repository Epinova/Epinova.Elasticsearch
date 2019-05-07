using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using EPiServer.DataAbstraction;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.Conventions
{
    public sealed partial class Indexing
    {
        internal static ConcurrentDictionary<string, List<BestBet>> BestBets;

        internal static void SetupBestBets()
        {
            BestBets = new ConcurrentDictionary<string, List<BestBet>>();

            var repository = ServiceLocator.Current.GetInstance<IBestBetsRepository>();
            var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
            var languageBranchRepository = ServiceLocator.Current.GetInstance<ILanguageBranchRepository>();

            var languageIds = languageBranchRepository.ListEnabled()
                .Select(lang => lang.LanguageID);

            var config = ElasticSearchSection.GetConfiguration();

            var indexList = config.IndicesParsed.ToList();

            if (settings.CommerceEnabled)
            {
                indexList.Add(new IndexConfiguration
                {
                    Name = $"{settings.Index}-{Constants.CommerceProviderName}".ToLower(),
                    DisplayName = "Commerce"
                });
            }

            foreach (var index in indexList)
            {
                Logger.Information($"Setup BestBets for index '{index.Name}'");
                foreach (var languageId in languageIds)
                {
                    Logger.Information($"Language '{languageId}'");
                    var indexName = index.Name + "-" + languageId;
                    var bestBets = repository.GetBestBets(languageId, indexName).ToList();
                    BestBets.TryAdd(indexName, bestBets);
                    Logger.Information($"BestBets:\n{String.Join("\n", bestBets.Select(b => b.Phrase + " => " + b.Id))}");
                }
}
        }
    }
}