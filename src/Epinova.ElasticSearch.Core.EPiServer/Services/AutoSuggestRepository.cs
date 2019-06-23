using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.EPiServer.Services
{
    [ServiceConfiguration(ServiceType = typeof(IAutoSuggestRepository), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class AutoSuggestRepository : IAutoSuggestRepository
    {
        private readonly IBlobFactory _blobFactory;
        private readonly IContentRepository _contentRepository;

        public AutoSuggestRepository(IContentRepository contentRepository, IBlobFactory blobFactory)
        {
            _contentRepository = contentRepository;
            _blobFactory = blobFactory;
        }

        public void AddWord(string languageId, string word)
        {
            List<string> words = GetWords(languageId);
            words.Add(word);
            SetWords(languageId, words.Distinct().ToList());
        }

        public void DeleteWord(string languageId, string word)
        {
            List<string> words = GetWords(languageId);
            words.Remove(word);
            SetWords(languageId, words.Where(w => !String.IsNullOrWhiteSpace(w)).Distinct().ToList());
        }

        private void SetWords(string languageId, IEnumerable<string> wordsToAdd)
        {
            var name = GetFilename(languageId);
            AutoSuggestFile contentFile = GetAutoSuggest(name);

            contentFile = contentFile == null
                ? _contentRepository.GetDefault<AutoSuggestFile>(GetAutoSuggestFolder().ContentLink)
                : contentFile.CreateWritableClone() as AutoSuggestFile;

            if(contentFile == null)
            {
                return;
            }

            string content = String.Join("|", wordsToAdd);

            var blob = _blobFactory.CreateBlob(contentFile.BinaryDataContainer, ".autosuggest");
            using(var stream = blob.OpenWrite())
            {
                var writer = new StreamWriter(stream);
                writer.Write(content);
                writer.Flush();
            }
            contentFile.Name = name;
            contentFile.BinaryData = blob;
            contentFile.LanguageId = languageId;

            var suggestRef = _contentRepository.Save(contentFile, SaveAction.Publish, AccessLevel.NoAccess);
            DeleteabAndonedDocuments(suggestRef, name, languageId);
        }

        public List<string> GetWords(string languageId)
        {
            var words = new List<string>();

            AutoSuggestFile backup = GetAutoSuggest(GetFilename(languageId));
            if(backup?.BinaryData == null)
            {
                return words;
            }

            using(var stream = backup.BinaryData.OpenRead())
            {
                using(var reader = new StreamReader(stream))
                {
                    words = reader.ReadToEnd().Split('|').ToList();
                }
            }

            return words;
        }

        private AutoSuggestFile GetAutoSuggest(string name)
        {
            ContentReference backupFolder = GetAutoSuggestFolder().ContentLink;

            return _contentRepository
                .GetChildren<AutoSuggestFile>(backupFolder)
                .FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetFilename(string languageId)
            => String.Concat(languageId, ".autosuggest");

        private AutoSuggestFileFolder GetAutoSuggestFolder(string folderName = "Elasticsearch Autosuggest")
        {
            PageReference parent = ContentReference.RootPage;

            AutoSuggestFileFolder backupFolder = _contentRepository.GetChildren<AutoSuggestFileFolder>(parent).FirstOrDefault();
            if(backupFolder == null)
            {
                backupFolder = _contentRepository.GetDefault<AutoSuggestFileFolder>(parent);
                backupFolder.Name = folderName;
                _contentRepository.Save(backupFolder, SaveAction.Publish, AccessLevel.NoAccess);
            }
            return backupFolder;
        }

        private void DeleteabAndonedDocuments(ContentReference current, string name, string languageId)
        {
            ContentReference folder = GetAutoSuggestFolder().ContentLink;
            var abandonedSuggestions = _contentRepository.GetChildren<AutoSuggestFile>(folder)
                .Where(b => !b.ContentLink.CompareToIgnoreWorkID(current)
                    && b.Name != name
                    && b.LanguageId == languageId)
                .Select(s => s.Name);
            abandonedSuggestions.ToList().ForEach(n =>
            {
                var content = GetAutoSuggest(n);
                if(content != null)
                {
                    _contentRepository.Delete(content.ContentLink, true, AccessLevel.NoAccess);
                }
            });
        }
    }
}