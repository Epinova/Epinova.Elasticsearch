using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Contracts;
using Epinova.ElasticSearch.Core.EPiServer.Models;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Logging;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.EPiServer.Services
{
    [ServiceConfiguration(ServiceType = typeof(ISynonymRepository), Lifecycle = ServiceInstanceScope.Hybrid)]
    public class SynonymRepository : ISynonymRepository
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(SynonymRepository));
        private readonly IBlobFactory _blobFactory;
        private readonly IContentRepository _contentRepository;
        private readonly IElasticSearchSettings _settings;
        private readonly IServerInfoService _serverInfoService;
        private readonly IHttpClientHelper _httpClientHelper;

        public SynonymRepository(
            IContentRepository contentRepository,
            IBlobFactory blobFactory,
            IElasticSearchSettings settings,
            IServerInfoService serverInfoService,
            IHttpClientHelper httpClientHelper)
        {
            _contentRepository = contentRepository;
            _blobFactory = blobFactory;
            _settings = settings;
            _serverInfoService = serverInfoService;
            _httpClientHelper = httpClientHelper;
        }

        public void SetSynonyms(string languageId, string analyzer, List<Synonym> synonymsToAdd, string index)
        {

            if(String.IsNullOrWhiteSpace(index))
            {
                index = _settings.GetDefaultIndexName(languageId);
            }

            var indexing = new Indexing(_serverInfoService, _settings, _httpClientHelper);
            indexing.Close(index);

            try
            {
                string[] synonymPairs = synonymsToAdd
                    .Select(s => String.Concat(s.From, s.MultiWord ? "=>" : ",", s.To))
                    .ToArray();

                if(synonymPairs.Length == 0)
                {
                    synonymPairs = new[] {"example_from,example_to"};
                }

                _logger.Information(
                    $"Adding {synonymsToAdd.Count} synonyms for language:{languageId} and analyzer:{analyzer}");

                if(_logger.IsDebugEnabled())
                {
                    synonymPairs.ToList().ForEach(pair => _logger.Debug(pair));
                }

                dynamic body = new
                {
                    settings = new
                    {
                        analysis = new
                        {
                            filter = new
                            {
                                ANALYZERTOKEN_synonym_filter = new
                                {
                                    type = "synonym", synonyms = synonymPairs
                                }
                            }
                        }
                    }
                };

                SaveBackup(languageId, index, synonymPairs);

                string json = Serialization.Serialize(body);

                json = json.Replace("ANALYZERTOKEN", analyzer);

                if(_logger.IsDebugEnabled())
                {
                    _logger.Debug("SYNONYM JSON PAYLOAD:\n" + json);
                }

                var data = Encoding.UTF8.GetBytes(json);
                var uri = indexing.GetUri(index, "_settings");

                _httpClientHelper.Put(uri, data);
            }
            catch(Exception ex)
            {
                _logger.Error($"Failure adding {synonymsToAdd.Count} synonyms for language:{languageId} and analyzer:{analyzer}", ex);
            }
            finally
            {
                indexing.Open(index);
            }
        }

        public string GetSynonymsFilePath(string languageId, string index)
        {
            if(String.IsNullOrWhiteSpace(index))
            {
                index = _settings.GetDefaultIndexName(languageId);
            }

            var indexing = new Indexing(_serverInfoService, _settings, _httpClientHelper);

            if(!indexing.IndexExists(index))
            {
                return null;
            }

            var json = _httpClientHelper.GetString(indexing.GetUri(index, "_settings"));

            var jpath = $"{index}.settings.index.analysis.filter.{Language.GetLanguageAnalyzer(languageId)}_synonym_filter.synonyms_path";

            JContainer settings = JsonConvert.DeserializeObject<JContainer>(json);
            return settings.SelectToken(jpath)?.ToString();
        }

        public List<Synonym> GetSynonyms(string languageId, string index)
        {
            var synonyms = new List<Synonym>();

            if(String.IsNullOrWhiteSpace(index))
            {
                index = _settings.GetDefaultIndexName(languageId);
            }

            var indexing = new Indexing(_serverInfoService, _settings, _httpClientHelper);

            if(!indexing.IndexExists(index))
            {
                return synonyms;
            }

            var json = _httpClientHelper.GetString(indexing.GetUri(index, "_settings"));

            var jpath = $"{index}.settings.index.analysis.filter.{Language.GetLanguageAnalyzer(languageId)}_synonym_filter.synonyms";

            JContainer settings = JsonConvert.DeserializeObject<JContainer>(json);
            JToken synonymPairs = settings.SelectToken(jpath);
            string[] parsedSynonyms;
            if(synonymPairs?.Any(s => s.ToString() != "example_from,example_to") == true)
            {
                parsedSynonyms = synonymPairs.Select(s => s.ToString()).ToArray();
            }
            else
            {
                SynonymBackupFile backup = GetBackup(GetFilename(languageId, index));
                if(backup?.BinaryData == null)
                {
                    return synonyms;
                }

                using(var stream = backup.BinaryData.OpenRead())
                {
                    using(var reader = new StreamReader(stream))
                    {
                        string data = reader.ReadToEnd();
                        _logger.Debug("Synonym data: " + data);
                        parsedSynonyms = data.Split('|');
                    }
                }
            }

            foreach(string synonym in parsedSynonyms)
            {
                if(String.IsNullOrWhiteSpace(synonym))
                {
                    continue;
                }

                var arrowPos = synonym.IndexOf("=>");
                var firstCommaPos = synonym.IndexOf(',');
                var isMultiword = arrowPos > firstCommaPos;
                var splitToken = new[] { isMultiword ? "=>" : "," };

                _logger.Debug("Synonym: " + synonym);

                var pair = synonym.Split(splitToken, StringSplitOptions.None);
                if(pair.Length > 1)
                {
                    synonyms.Add(new Synonym
                    {
                        From = pair[0],
                        To = pair[1],
                        TwoWay = !isMultiword && !pair[0].Contains("=>"),
                        MultiWord = isMultiword
                    });
                }
            }

            return synonyms;
        }

        private SynonymBackupFile GetBackup(string name)
        {
            ContentReference backupFolder = GetBackupFolder().ContentLink;

            SynonymBackupFile backupFile = _contentRepository
                .GetChildren<SynonymBackupFile>(backupFolder)
                .FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if(backupFile == null)
            {
                return null;
            }

            if(backupFile.BinaryData is FileBlob fileBlob && !File.Exists(fileBlob.FilePath))
            {
                return null;
            }

            return backupFile;
        }

        private void SaveBackup(string languageId, string index, string[] synonymPairs)
        {
            var name = GetFilename(languageId, index);
            SynonymBackupFile contentFile = GetBackup(name);

            //TODO: Don't create new version for identical files.

            contentFile = contentFile == null
                ? _contentRepository.GetDefault<SynonymBackupFile>(GetBackupFolder().ContentLink)
                : contentFile.CreateWritableClone() as SynonymBackupFile;

            var content = String.Join("|", synonymPairs);

            var blob = _blobFactory.CreateBlob(contentFile.BinaryDataContainer, ".synonyms");
            using(var stream = blob.OpenWrite())
            {
                var writer = new StreamWriter(stream);
                writer.Write(content);
                writer.Flush();
            }
            contentFile.Name = name;
            contentFile.BinaryData = blob;

            _contentRepository.Save(contentFile, SaveAction.Publish, AccessLevel.NoAccess);

            if(_logger.IsDebugEnabled())
            {
                _logger.Debug("SaveBackup -> Name: " + contentFile.Name);
                _logger.Debug("SaveBackup -> RouteSegment: " + contentFile.RouteSegment);
                _logger.Debug("SaveBackup -> MimeType: " + contentFile.MimeType);
                _logger.Debug("SaveBackup -> ContentLink: " + contentFile.ContentLink);
                _logger.Debug("SaveBackup -> Status: " + contentFile.Status);
            }
        }

        private static string GetFilename(string languageId, string index)
            => $"{languageId}_{index}.synonyms";

        private SynonymBackupFileFolder GetBackupFolder(string folderName = "Elasticsearch Synonyms")
        {
            var parent = ContentReference.RootPage;

            var backupFolder = _contentRepository.GetChildren<SynonymBackupFileFolder>(parent).FirstOrDefault();
            if(backupFolder == null)
            {
                backupFolder = _contentRepository.GetDefault<SynonymBackupFileFolder>(parent);
                backupFolder.Name = folderName;
                _contentRepository.Save(backupFolder, SaveAction.Publish, AccessLevel.NoAccess);
            }

            return backupFolder;
        }
    }
}