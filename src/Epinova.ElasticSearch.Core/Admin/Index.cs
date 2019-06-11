using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Admin;
using Epinova.ElasticSearch.Core.Models.Serialization;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Admin
{
    public class Index
    {
        private readonly string _name;
        private readonly string _language;
        private readonly Indexing _indexing;
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(Index));
        private static IElasticSearchSettings _settings;

        public Index(IElasticSearchSettings settings, string name) : this(settings)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("'name' can not be empty.");

            _name = name.ToLower();
            _language = _name.Split('-').Last();
        }

        internal Index(IElasticSearchSettings settings)
        {
            _settings = settings;
            _indexing = new Indexing(settings);
        }

        public virtual IEnumerable<IndexInformation> GetIndices()
        {
            string uri = $"{_settings.Host}/_cat/indices?format=json";
            string json = HttpClientHelper.GetJson(new Uri(uri));

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

            string languageAnalyzer = Language.GetLanguageAnalyzer(_settings.GetLanguage(_name));

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
                string response = HttpClientHelper.GetString(new Uri(uri));

                IndexStatus indexStatus = JsonConvert.DeserializeObject<IndexStatus>(response);

                bool success = !indexStatus.TimedOut && status.Contains(indexStatus.Status);

                Logger.Debug($"Success: {success}. Timeout: {indexStatus.TimedOut}. Status: {indexStatus.Status}");

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error("Could not get status", ex);
                return false;
            }
        }

        internal int GetDocumentCount()
        {
            var uri = _indexing.GetUri(_name, "_search") + "?size=0";
            dynamic model = new { hits = new { total = 0 } };

            try
            {
                string response = HttpClientHelper.GetString(new Uri(uri));
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
            string typeName = type?.FullName ?? "Unknown/Custom";

            Logger.Information($"Initializing index. Type: {typeName}. Name: {_name}. Language: {_language}");

            EnableClosing();

            _indexing.CreateIndex(_name);

            CreateStandardSettings();
            CreateAttachmentPipeline();

            _indexing.Close(_name);

            CreateTriGramTokenizer();
            CreateRawAnalyzer();
            CreateShingleFilter();
            CreateSynonymSettings();
            CreateSuggestAnalyzer();
            CreateShingleSettings();

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
            string typeName = indexType.GetTypeName();
            string json = MappingPatterns.GetDisableDynamicMapping(typeName);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_mapping", typeName);

            Logger.Information($"Disable dynamic mapping for {typeName}");
            Logger.Information($"PUT: {uri}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        internal void ChangeTokenizer(string tokenizer)
        {
            dynamic body = MappingPatterns.GetTokenizerTemplate(_settings.GetLanguage(_name), tokenizer);
            string json = Serialization.Serialize(body);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");
            HttpClientHelper.Put(uri, data);

            Logger.Information($"Adding tri-gram tokenizer:\n{json}");
        }

        private void CreateCustomMappings(Type type)
        {
            string json = Serialization.Serialize(MappingPatterns.GetCustomIndexMapping(Language.GetLanguageAnalyzer(_language)));
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_mapping", type.GetTypeName());

            Logger.Information($"Creating custom mappings. Language: {_language}");
            Logger.Information($"PUT: {uri}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateStandardMappings()
        {
            string json = Serialization.Serialize(MappingPatterns.GetStandardIndexMapping(Language.GetLanguageAnalyzer(_language)));
            byte[] data = Encoding.UTF8.GetBytes(json); 
            var uri = _indexing.GetUri(_name, "_mapping", typeof(IndexItem).GetTypeName());

            Logger.Information($"Creating standard mappings. Language: {_language}");
            Logger.Information($"PUT: {uri}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateStandardSettings()
        {
            string json = Serialization.Serialize(MappingPatterns.DefaultSettings);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information($"Creating standard settings. Language: {_name}");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateAttachmentPipeline()
        {
            string json = Serialization.Serialize(Pipelines.Attachment.Definition);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = new Uri($"{_settings.Host}/_ingest/pipeline/{Pipelines.Attachment.Name}");

            Logger.Information("Creating Attachment Pipeline");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateTriGramTokenizer()
        {
            string json = Serialization.Serialize(Analyzers.TriGramTokenizer);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information("Adding tri-gram tokenizer");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateShingleFilter()
        {
            string json = Serialization.Serialize(Analyzers.Shingle);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information("Adding Shingle filter");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateRawAnalyzer()
        {
            string json = Serialization.Serialize(Analyzers.Raw);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");
            HttpClientHelper.Put(uri, data);

            Logger.Information($"Adding raw analyzer:\n{JToken.Parse(json).ToString(Formatting.Indented)}");
        }

        private void CreateSuggestAnalyzer()
        {
            string json = Serialization.Serialize(Analyzers.GetSuggestAnalyzer(Language.GetLanguageAnalyzer(_language)));
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information("Adding Suggest analyzer");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateSynonymSettings()
        {
            string languageName = Language.GetLanguageAnalyzer(_language);

            if (languageName == null || !Analyzers.List.ContainsKey(languageName))
            {
                Logger.Warning($"No analyzer with synonyms found for '{languageName}'");
                return;
            }

            KeyValuePair<string, dynamic> analyzer = Analyzers.List.First(a => a.Key == languageName);

            string json = Serialization.Serialize(analyzer.Value);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information("Adding analyzer with synonyms");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void CreateShingleSettings()
        {
            string languageName = Language.GetLanguageAnalyzer(_language);
            string key = languageName + "_suggest";

            if (languageName == null || !Analyzers.List.ContainsKey(key))
            {
                Logger.Warning($"No shingle analyzer found for '{languageName}'");
                return;
            }

            KeyValuePair<string, dynamic> analyzer = Analyzers.List.First(a => a.Key == key);

            string json = Serialization.Serialize(analyzer.Value);
            byte[] data = Encoding.UTF8.GetBytes(json);
            var uri = _indexing.GetUri(_name, "_settings");

            Logger.Information("Adding shingle settings");
            Logger.Information(JToken.Parse(json).ToString(Formatting.Indented));

            HttpClientHelper.Put(uri, data);
        }

        private void EnableClosing()
        {
            dynamic body = MappingPatterns.IndexClosing;
            string json = Serialization.Serialize(body);
            byte[] data = Encoding.UTF8.GetBytes(json);

            var uri = new Uri(String.Concat(_settings.Host.TrimEnd('/'), "/_cluster/settings"));
            HttpClientHelper.Put(uri, data);

            Logger.Information($"Enabling cluster index closing:\n{json}");
        }

        private static bool MatchName(IndexInformation i)
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
