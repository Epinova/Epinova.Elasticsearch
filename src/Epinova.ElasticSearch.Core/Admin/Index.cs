using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Models.Serialization;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Settings.Configuration;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Admin
{
    public class Index
    {
        private readonly string _name;
        private readonly string _nameWithoutLanguage;
        private readonly string _language;
        private readonly Indexing _indexing;
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(Index));
        private readonly IElasticSearchSettings _settings;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly Mapping _mapping;

        public Index(IServerInfoService serverInfoService, IElasticSearchSettings settings, IHttpClientHelper httpClientHelper, string name)
        {
            _httpClientHelper = httpClientHelper;
            _settings = settings;
            _indexing = new Indexing(serverInfoService, settings, httpClientHelper);
            _mapping = new Mapping(serverInfoService, settings, httpClientHelper);

            if(!string.IsNullOrWhiteSpace(name))
            {
                _name = name.ToLower();
                _language = _settings.GetLanguageFromIndexName(_name);
                _nameWithoutLanguage = _settings.GetIndexNameWithoutLanguage(_name);
            }
        }

        public virtual IEnumerable<IndexInformation> GetIndices()
        {
            var uri = $"{_settings.Host}/_cat/indices?format=json";
            var json = _httpClientHelper.GetJson(new Uri(uri));

            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var indices = JsonConvert.DeserializeObject<IndexInformation[]>(json, serializerSettings)
                .Where(MatchName)
                .ToArray();

            foreach(var indexInfo in indices)
            {
                indexInfo.Tokenizer = GetTokenizer(indexInfo.Index);
            }

            return indices;
        }

        internal bool Exists => _indexing.IndexExists(_name);

        internal string GetTokenizer(string name)
        {
            if(String.IsNullOrWhiteSpace(name) || !_indexing.IndexExists(name))
            {
                return String.Empty;
            }

            Uri uri = _indexing.GetUri(name, "_settings");
            var json = _httpClientHelper.GetString(uri);
            var languageAnalyzer = Language.GetLanguageAnalyzer(_settings.GetLanguageFromIndexName(name));

            if(string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(languageAnalyzer))
                return String.Empty;

            var jpath = $"{name}.settings.index.analysis.analyzer.{languageAnalyzer}.tokenizer";

            JContainer settings = JsonConvert.DeserializeObject<JContainer>(json);

            JToken token = settings?.SelectToken(jpath);
            if(token == null)
            {
                return String.Empty;
            }

            return token.ToString();
        }

        internal bool WaitForStatus(int timeout = 10, string status = "yellow")
        {
            var uri = $"{_settings.Host}/_cluster/health/{_name}?wait_for_status={status}&timeout={timeout}s";

            _logger.Debug($"Waiting {timeout}s for status '{status}' on index '{_name}'");

            try
            {
                var response = _httpClientHelper.GetString(new Uri(uri));

                IndexStatus indexStatus = JsonConvert.DeserializeObject<IndexStatus>(response);

                var isSuccess = !indexStatus.TimedOut && status.Contains(indexStatus.Status);

                _logger.Debug($"Success: {isSuccess}. Timeout: {indexStatus.TimedOut}. Status: {indexStatus.Status}");

                return isSuccess;
            }
            catch(Exception ex)
            {
                _logger.Error("Could not get status", ex);
                return false;
            }
        }

        internal void Initialize(Type type)
        {
            var typeName = type?.FullName ?? "Unknown/Custom";

            _logger.Information($"Initializing index. Type: {typeName}. Name: {_name}. Language: {_language}");

            EnableClosing();

            _indexing.CreateIndex(_name);

            CreateStandardSettings();
            CreateAttachmentPipeline();

            _indexing.Close(_name);

            CreateAnalyzerSettings();

            _indexing.Open(_name);

            if(type != null)
            {
                if(type == typeof(IndexItem))
                {
                    CreateStandardMappings();
                }
                else
                {
                    CreateCustomMappings();
                }
            }
        }

        internal void ChangeTokenizer(string tokenizer)
        {
            dynamic body = MappingPatterns.GetTokenizerTemplate(_settings.GetLanguageFromIndexName(_name), tokenizer);
            var json = Serialization.Serialize(body);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");
            _httpClientHelper.Put(uri, data);

            _logger.Information($"Adding tri-gram tokenizer:\n{json}");
        }

        private void CreateCustomMappings()
        {
            string json = Serialization.Serialize(MappingPatterns.GetCustomIndexMapping(Language.GetLanguageAnalyzer(_language)));
            byte[] data = Encoding.UTF8.GetBytes(json);

            Uri uri = _mapping.GetMappingUri(_name);

            _logger.Information($"Creating custom mappings. Language: {_language}");
            _logger.Information($"PUT: {uri}");
            _logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Put(uri, data);
        }

        private void CreateStandardMappings()
        {
            string json = Serialization.Serialize(MappingPatterns.GetStandardIndexMapping(Language.GetLanguageAnalyzer(_language)));
            var data = Encoding.UTF8.GetBytes(json);

            Uri uri = _mapping.GetMappingUri(_name);

            _logger.Information($"Creating standard mappings. Language: {_language}");
            _logger.Information($"PUT: {uri}");
            _logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Put(uri, data);
        }

        private void CreateStandardSettings()
        {
            string json = Serialization.Serialize(MappingPatterns.DefaultSettings);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            _logger.Information($"Creating standard settings. Language: {_name}");
            _logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Put(uri, data);
        }

        private void CreateAnalyzerSettings()
        {
            var config = ElasticSearchSection.GetConfiguration();
            var indexConfig = config.IndicesParsed.FirstOrDefault(i => i.Name == _nameWithoutLanguage);

            string json = Serialization.Serialize(Analyzers.GetAnalyzerSettings(_language, indexConfig?.SynonymsFile));
            var data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            _logger.Information($"Creating analyzer settings. Language: {_name}");
            _logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Put(uri, data);
        }

        private void CreateAttachmentPipeline()
        {
            string json = Serialization.Serialize(Pipelines.Attachment.Definition);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = new Uri($"{_settings.Host}/_ingest/pipeline/{Pipelines.Attachment.Name}");

            _logger.Information("Creating Attachment Pipeline");
            _logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            _httpClientHelper.Put(uri, data);
        }

        private void EnableClosing()
        {
            dynamic body = MappingPatterns.IndexClosing;
            var json = Serialization.Serialize(body);
            var data = Encoding.UTF8.GetBytes(json);

            var uri = new Uri(String.Concat(_settings.Host.TrimEnd('/'), "/_cluster/settings"));
            _httpClientHelper.Put(uri, data);

            _logger.Information($"Enabling cluster index closing:\n{json}");
        }

        private bool MatchName(IndexInformation i) => _settings.Indices.Any(index => i.Index.StartsWith(index, StringComparison.OrdinalIgnoreCase) && i.Index.Contains("¤"));
    }
}
