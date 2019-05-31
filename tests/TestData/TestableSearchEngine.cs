﻿using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Epinova.ElasticSearch.Core.Engine;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Query;
using Epinova.ElasticSearch.Core.Settings;

namespace TestData
{
    internal class TestableSearchEngine : SearchEngine
    {
        private readonly string[] _suggestions;
        private readonly string _jsonFile;

        public TestableSearchEngine(string jsonFile, IElasticSearchSettings settings)
            : base(settings)
        {
            _jsonFile = jsonFile;
        }

        public TestableSearchEngine(string[] suggestions, IElasticSearchSettings settings)
            : base(settings)
        {
            _suggestions = suggestions;
        }

        public override string[] GetSuggestions(SuggestRequest request, CultureInfo culture, string indexName = null)
        {
            return _suggestions ?? base.GetSuggestions(request, culture, indexName);
        }

        public override JsonReader GetResponse(RequestBase request, string endpoint, out string rawJsonResult)
        {
            rawJsonResult = null;
            return new JsonTextReader(new StringReader(Factory.GetJsonTestData(_jsonFile)));
        }
    }
}