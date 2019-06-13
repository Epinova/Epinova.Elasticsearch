using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Extensions;
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
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Index));
        private readonly IElasticSearchSettings _settings;

        public Index(IElasticSearchSettings settings, string name) : this(settings)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("'name' can not be empty.");

            _name = name.ToLower();
            _language = _name.Split('-').Last();
            _nameWithoutLanguage = _name.Substring(0, _name.Length - _language.Length - 1);
        }

        internal Index(IElasticSearchSettings settings)
        {
            _settings = settings;
            _indexing = new Indexing(settings);
        }

        public virtual IEnumerable<IndexInformation> GetIndices()
        {
            var uri = $"{_settings.Host}/_cat/indices?format=json";
            var json = HttpClientHelper.GetJson(new Uri(uri));

            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };

            var indices = JsonConvert.DeserializeObject<IndexInformation[]>(json, serializerSettings)
                .Where(MatchName)
                .ToArray();

            foreach (var indexInfo in indices)
            {
                var index = new Index(_settings, indexInfo.Index);
                indexInfo.Tokenizer = index.GetTokenizer();
            }

            return indices;
        }

        internal bool Exists => _indexing.IndexExists(_name);

        internal string GetTokenizer()
        {
            if (String.IsNullOrWhiteSpace(_name) || !_indexing.IndexExists(_name))
                return String.Empty;

            var json = HttpClientHelper.GetString(_indexing.GetUri(_name, "_settings"));
            var languageAnalyzer = Language.GetLanguageAnalyzer(_settings.GetLanguage(_name));

            if (String.IsNullOrWhiteSpace(languageAnalyzer))
                return String.Empty;

            var jpath = $"{_name}.settings.index.analysis.analyzer.{languageAnalyzer}.tokenizer";

            JContainer settings = JsonConvert.DeserializeObject<JContainer>(json);

            JToken token = settings?.SelectToken(jpath);
            if (token == null)
                return String.Empty;

            return token.ToString();
        }

        internal bool WaitForStatus(int timeout = 10, string status = "yellow")
        {
            var uri = $"{_settings.Host}/_cluster/health/{_name}?wait_for_status={status}&timeout={timeout}s";

            Logger.Debug($"Waiting {timeout}s for status '{status}' on index '{_name}'");

            try
            {
                var response = HttpClientHelper.GetString(new Uri(uri));

                IndexStatus indexStatus = JsonConvert.DeserializeObject<IndexStatus>(response);

                var isSuccess = !indexStatus.TimedOut && status.Contains(indexStatus.Status);

                Logger.Debug($"Success: {isSuccess}. Timeout: {indexStatus.TimedOut}. Status: {indexStatus.Status}");

                return isSuccess;
            }
            catch (Exception ex)
            {
                Logger.Error("Could not get status", ex);
                return false;
            }
        }

        internal int GetDocumentCount()
        {
            var uri = _indexing.GetUri(_name, "_search", null, "size=0&rest_total_hits_as_int=true");
            dynamic model = new { hits = new { total = 0 } };

            try
            {
                var response = HttpClientHelper.GetString(new Uri(uri));
                var result = JsonConvert.DeserializeAnonymousType(response, model);
                return result.hits.total;
            }
            catch (Exception ex)
            {
                Logger.Error("Could not get count", ex);
                return 0;
            }
        }

        internal void Initialize(Type type)
        {
            var typeName = type?.FullName ?? "Unknown/Custom";

            Logger.Information($"Initializing index. Type: {typeName}. Name: {_name}. Language: {_language}");

            EnableClosing();

            _indexing.CreateIndex(_name);

            CreateStandardSettings();
            CreateAttachmentPipeline();

            _indexing.Close(_name);

            CreateAnalyzerSettings();

            _indexing.Open(_name);

            if (type != null)
            {
                if (type == typeof(IndexItem))
                    CreateStandardMappings();
                else
                    CreateCustomMappings(type);
            }
        }

        internal void DisableDynamicMapping(Type indexType)
        {
            var typeName = indexType.GetTypeName();
            var json = MappingPatterns.GetDisableDynamicMapping(typeName);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_mapping", typeName, "include_type_name=true");

            Logger.Information($"Disable dynamic mapping for {typeName}");
            Logger.Information($"PUT: {uri}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        internal void ChangeTokenizer(string tokenizer)
        {
            dynamic body = MappingPatterns.GetTokenizerTemplate(_settings.GetLanguage(_name), tokenizer);
            var json = Serialization.Serialize(body);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");
            HttpClientHelper.Put(uri, data);

            Logger.Information($"Adding tri-gram tokenizer:\n{json}");
        }

        private void CreateCustomMappings(Type type)
        {
            string json = Serialization.Serialize(MappingPatterns.GetCustomIndexMapping(Language.GetLanguageAnalyzer(_language)));
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_mapping", type.GetTypeName(), "include_type_name=true");

            Logger.Information($"Creating custom mappings. Language: {_language}");
            Logger.Information($"PUT: {uri}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateStandardMappings()
        {
            string json = Serialization.Serialize(MappingPatterns.GetStandardIndexMapping(Language.GetLanguageAnalyzer(_language)));
            var data = Encoding.UTF8.GetBytes(json); 
            var uri = _indexing.GetUri(_name, "_mapping", typeof(IndexItem).GetTypeName(), "include_type_name=true");

            Logger.Information($"Creating standard mappings. Language: {_language}");
            Logger.Information($"PUT: {uri}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateStandardSettings()
        {
            string json = Serialization.Serialize(MappingPatterns.DefaultSettings);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information($"Creating standard settings. Language: {_name}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateAnalyzerSettings()
        {
            var config = ElasticSearchSection.GetConfiguration();
            var indexConfig = config.IndicesParsed.FirstOrDefault(i => i.Name == _nameWithoutLanguage);

            string json = Serialization.Serialize(Analyzers.GetAnalyzerSettings(_language, indexConfig?.SynonymsFile));
            var data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information($"Creating analyzer settings. Language: {_name}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateAttachmentPipeline()
        {
            string json = Serialization.Serialize(Pipelines.Attachment.Definition);
            var data = Encoding.UTF8.GetBytes(json);
            var uri = new Uri($"{_settings.Host}/_ingest/pipeline/{Pipelines.Attachment.Name}");

            Logger.Information("Creating Attachment Pipeline");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void EnableClosing()
        {
            dynamic body = MappingPatterns.IndexClosing;
            var json = Serialization.Serialize(body);
            var data = Encoding.UTF8.GetBytes(json);

            var uri = new Uri(String.Concat(_settings.Host.TrimEnd('/'), "/_cluster/settings"));
            HttpClientHelper.Put(uri, data);

            Logger.Information($"Enabling cluster index closing:\n{json}");
        }

        private bool MatchName(IndexInformation i)
        {
            foreach (string indexName in _settings.Indices)
            {
                if (i.Index.StartsWith(String.Concat(indexName, "-"), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}
