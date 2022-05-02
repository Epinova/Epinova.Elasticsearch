using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace Epinova.ElasticSearch.Core.EPiServer.Services
{
    [ServiceConfiguration(ServiceType = typeof(IBestBetsRepository), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class BestBetsRepository : IBestBetsRepository
    {
        private const char PhraseDelim = '¤';
        private const string RowDelim = "|";
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(BestBetsRepository));
        private readonly IBlobFactory _blobFactory;
        private readonly ICoreIndexer _coreIndexer;
        private readonly IContentRepository _contentRepository;
        private readonly IElasticSearchSettings _settings;
        private readonly UrlResolver _urlResolver;

        public BestBetsRepository(
            IContentRepository contentRepository,
            IElasticSearchSettings settings,
            UrlResolver urlResolver,
            IBlobFactory blobFactory,
            ICoreIndexer coreIndexer)
        {
            _contentRepository = contentRepository;
            _settings = settings;
            _urlResolver = urlResolver;
            _blobFactory = blobFactory;
            _coreIndexer = coreIndexer;
        }

        public void AddBestBet(CultureInfo language, string phrase, ContentReference contentLink, string index, Type type)
        {
            List<BestBet> bestBets = GetBestBets(language, index).ToList();
            bestBets.Add(new BestBet(phrase, contentLink));
            SetBestBets(language, bestBets.ToArray(), index, type);
        }

        public void DeleteBestBet(CultureInfo language, string phrase, string id, string index, Type type)
        {
            List<BestBet> bestBets = GetBestBets(language, index).ToList();
            BestBet target = bestBets.FirstOrDefault(b => b.Phrase == phrase && b.Id == id);

            if(target == null)
            {
                return;
            }

            bestBets.Remove(target);
            var result = bestBets.Where(b => !String.IsNullOrWhiteSpace(b.Phrase));
            SetBestBets(language, result.ToArray(), index, type);
            _coreIndexer.ClearBestBets(index, type, id);
        }

        public IEnumerable<string> GetBestBetsForContent(CultureInfo language, int contentId, string index, bool isCommerceContent = false)
        {
            var id = isCommerceContent ? $"{contentId}__{Constants.CommerceProviderName}" : $"{contentId}";

            return GetBestBets(language, index)
                .Where(b => b.Id == id)
                .SelectMany(b => b.GetTerms());
        }

        public IEnumerable<BestBet> GetBestBets(CultureInfo language, string index)
        {
            var backup = GetBestBetsFile(GetFilename(language, index));
            if(backup?.BinaryData == null)
            {
                return Enumerable.Empty<BestBet>();
            }

            try
            {
                using(var stream = backup.BinaryData.OpenRead())
                {
                    using(var reader = new StreamReader(stream))
                    {
                        string raw = reader.ReadToEnd();

                        Logger.Information($"Raw data:\n{raw}");

                        if(String.IsNullOrWhiteSpace(raw))
                        {
                            return Enumerable.Empty<BestBet>();
                        }

                        return raw.Split(RowDelim[0])
                            .Where(IsValidRow)
                            .Select(b => ParseRow(b, language))
                            .Where(b => b != null);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Warning($"Failed to load BestBet file {backup?.Name}", ex);
                return Enumerable.Empty<BestBet>();
            }
        }

        private static bool IsValidRow(string row)
        {
            return !String.IsNullOrWhiteSpace(row)
                   && row.Contains(PhraseDelim);
        }

        private BestBet ParseRow(string row, CultureInfo language)
        {
            string[] parts = row.Split(PhraseDelim);
            string rawId = parts[1];
            var phrase = parts[0];
            var contentLink = ContentReference.Parse(rawId);
            var url = _urlResolver.GetUrl(contentLink, language.Name);

            return new BestBet(phrase, contentLink, url);
        }

        private void SetBestBets(CultureInfo language, BestBet[] bestBetsToAdd, string index, Type type)
        {
            var name = GetFilename(language, index);
            var contentFile = GetBestBetsFile(name);

            contentFile = contentFile == null
                ? _contentRepository.GetDefault<BestBetsFile>(GetBestBetsFolder().ContentLink)
                : contentFile.CreateWritableClone() as BestBetsFile;

            if(contentFile == null)
            {
                return;
            }

            Logger.Information($"Saving BestBest for language:{language.Name}");

            using(Stream stream = contentFile.BinaryData?.OpenRead() ?? Stream.Null)
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, (int)stream.Length);
                string filecontents = Encoding.UTF8.GetString(data);
                Logger.Information($"Old content:\n{filecontents}");
            }

            string content = String.Join(RowDelim,
                bestBetsToAdd.Select(PhraseToRow));

            Logger.Information($"New content:\n{content}");

            Blob blob = _blobFactory.CreateBlob(contentFile.BinaryDataContainer, "." + BestBetsFile.Extension);
            using(Stream stream = blob.OpenWrite())
            {
                var writer = new StreamWriter(stream);
                writer.Write(content);
                writer.Flush();
            }

            contentFile.Name = name;
            contentFile.BinaryData = blob;
            contentFile.Language = language;

            _contentRepository.Save(contentFile, SaveAction.Publish, AccessLevel.NoAccess);
            
            UpdateIndex(bestBetsToAdd, index, type);
        }

        private static string PhraseToRow(BestBet bestBet)
            => $"{bestBet.Phrase}{PhraseDelim}{bestBet.Id}{PhraseDelim}{bestBet.Provider}";

        private void UpdateIndex(in IEnumerable<BestBet> bestbets, string index, Type type)
        {
            var termsById = bestbets
                .GroupBy(b => b.Id)
                .Select(x => new
                {
                    Id = x.Key,
                    Terms = x.SelectMany(z => z.GetTerms()).ToArray()
                });

            foreach(var item in termsById)
            {
                _coreIndexer.UpdateBestBets(index, type, item.Id, item.Terms);
            }
        }

        private BestBetsFile GetBestBetsFile(string name)
        {
            var backupFolder = GetBestBetsFolder().ContentLink;

            return _contentRepository
                .GetChildren<BestBetsFile>(backupFolder)
                .FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase) || name.Replace(Constants.IndexNameLanguageSplitChar, '-').Equals(s.Name));
        }

        private static string GetFilename(CultureInfo language, string index) => $"{language.Name}_{index}.{BestBetsFile.Extension}";

        private BestBetsFileFolder GetBestBetsFolder()
        {
            var parent = ContentReference.RootPage;

            var backupFolder = _contentRepository.GetChildren<BestBetsFileFolder>(parent).FirstOrDefault();
            if(backupFolder == null)
            {
                backupFolder = _contentRepository.GetDefault<BestBetsFileFolder>(parent);
                backupFolder.Name = BestBetsFileFolder.ContentName;
                _contentRepository.Save(backupFolder, SaveAction.Publish, AccessLevel.NoAccess);
            }

            return backupFolder;
        }
    }
}
