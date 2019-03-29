using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.Extensions;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Query;
using Epinova.ElasticSearch.Core.Models.Serialization;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Aggregation = Epinova.ElasticSearch.Core.Models.Serialization.Aggregation;

namespace Epinova.ElasticSearch.Core.Engine
{
    internal class SearchEngine
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(SearchEngine));
        private static IElasticSearchSettings _elasticSearchSettings;

        internal SearchEngine()
        {
            _elasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
        }

        internal SearchEngine(IElasticSearchSettings settings)
        {
            _elasticSearchSettings = settings;
        }

        /// <summary>
        /// Execute the provided query
        /// </summary>
        /// <param name="query">Use <see cref="QueryBuilder"/> to generate a suitable query.</param>
        /// <param name="culture"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="indexName"></param>
        public async Task<SearchResult> QueryAsync(RequestBase query, CultureInfo culture, CancellationToken cancellationToken, string indexName = null)
        {
            if (query == null)
                return new SearchResult();

            EsRootObject results = await GetRawResultsAsync<EsRootObject>(query, Language.GetLanguageCode(culture), cancellationToken, indexName);
            if (results == null)
                return new SearchResult();

            var rawResults = new RawResults<EsRootObject> { RootObject = results };

            SearchResult searchResult = SetupResults(rawResults, query.ToString(Formatting.Indented));
            SetupFacets(results, searchResult);

            return searchResult;
        }

        /// <summary>
        /// Execute the provided query
        /// </summary>
        /// <param name="query">Use <see cref="QueryBuilder"/> to generate a suitable query.</param>
        /// <param name="culture"></param>
        /// <param name="indexName"></param>
        public SearchResult Query(RequestBase query, CultureInfo culture, string indexName = null)
        {
            if (query == null)
                return new SearchResult();

            RawResults<EsRootObject> rawResults = GetRawResults<EsRootObject>(query, Language.GetLanguageCode(culture), indexName);

            if (rawResults?.RootObject == null)
                return new SearchResult();

            SearchResult searchResult = SetupResults(rawResults, query.ToString(Formatting.Indented));

            SetupFacets(rawResults.RootObject, searchResult);

            return searchResult;
        }

        public async Task<CustomSearchResult<T>> CustomQueryAsync<T>(RequestBase query, CultureInfo culture, CancellationToken cancellationToken, string indexName = null)
        {
            if (query == null)
                return new CustomSearchResult<T>();

            EsCustomRootObject<T> rawResults = await GetRawResultsAsync<EsCustomRootObject<T>>(query, Language.GetLanguageCode(culture), cancellationToken, indexName);

            if (rawResults == null)
                return new CustomSearchResult<T>();

            if (rawResults.Hits?.HitArray == null || rawResults.Hits.HitArray.Length == 0)
                return new CustomSearchResult<T>();

            var searchResult = new SearchResult
            {
                Query = query.ToString(Formatting.Indented),
                TotalHits = rawResults.Hits.Total,
                Took = rawResults.Took
            };

            IEnumerable<CustomSearchHit<T>> searchHits = rawResults.Hits.HitArray.Select(h => new CustomSearchHit<T>(h.Source, h.Score, h.Highlight));
            var customSearchResult = new CustomSearchResult<T>(searchResult, searchHits);
            SetupFacets(rawResults, customSearchResult);

            return customSearchResult;
        }

        public CustomSearchResult<T> CustomQuery<T>(RequestBase query, CultureInfo culture, string indexName = null)
        {
            if (query == null)
                return new CustomSearchResult<T>();

            RawResults<EsCustomRootObject<T>> rawResults = GetRawResults<EsCustomRootObject<T>>(query, Language.GetLanguageCode(culture), indexName);
            if (rawResults?.RootObject == null)
                return new CustomSearchResult<T>();

            var searchResult = new CustomSearchResult<T>
            {
                Query = query.ToString(Formatting.Indented)
            };

            if (rawResults.RootObject.Hits?.HitArray != null && rawResults.RootObject.Hits.HitArray.Length > 0)
            {
                searchResult.Hits = rawResults.RootObject.Hits.HitArray.Select(h => new CustomSearchHit<T>(h.Source, h.Score, h.Highlight));
                searchResult.TotalHits = rawResults.RootObject.Hits.Total;
                searchResult.Took = rawResults.RootObject.Took;
            }

            SetupFacets(rawResults.RootObject, searchResult);

            return searchResult;
        }

        private SearchResult SetupResults(RawResults<EsRootObject> results, string query)
        {
            Hits hits = results.RootObject.Hits;
            var searchResult = new SearchResult
            {
                Query = query,
                RawJsonOutput = results.RawJson
            };

            if (results.RootObject.Suggest?.DidYouMean != null && results.RootObject.Suggest.DidYouMean.Length > 0)
                searchResult.DidYouMeanSuggestions = results.RootObject.Suggest.DidYouMean[0].Options;

            if (hits?.HitArray != null && hits.HitArray.Length > 0)
            {
                searchResult.Hits = hits.HitArray.Select(Map);
                searchResult.TotalHits = hits.Total;
                searchResult.Took = results.RootObject.Took;
            }

            return searchResult;
        }

        private static SearchHit Map(Hit hit)
        {
            var searchHit = new SearchHit(hit);

            CustomProperty[] customPropertiesForType =
                hit.Source?.Types != null
                    ? Conventions.Indexing.CustomProperties.Where(c => hit.Source.Types.Contains(c.OwnerType.GetTypeName())).ToArray()
                    : Enumerable.Empty<CustomProperty>().ToArray();

            if (customPropertiesForType.Length == 0)
                return searchHit;

            if (hit.Source?.UnmappedFields == null || !hit.Source.UnmappedFields.Any(u => customPropertiesForType.Any(c => c.Name == u.Key)))
                return searchHit;

            foreach (CustomProperty property in customPropertiesForType)
            {
                if (!hit.Source.UnmappedFields.ContainsKey(property.Name))
                    continue;

                JToken unmappedField = hit.Source.UnmappedFields[property.Name];
                if (unmappedField == null)
                    break;

                // Array value
                if (unmappedField.Children().Any())
                {
                    searchHit.CustomProperties[property.Name] = unmappedField.Children().Cast<JValue>().Select(v => v.Value).ToArray();
                    continue;
                }

                if (unmappedField is JValue value)
                    searchHit.CustomProperties[property.Name] = value.Value;
            }

            return searchHit;
        }

        public RawResults<TRoot> GetRawResults<TRoot>(RequestBase query, string language, string indexName = null)
        {
            if (indexName == null)
                indexName = _elasticSearchSettings.GetDefaultIndexName(language);

            Logger.Information($"Index:\n{indexName}\n");
            Logger.Information($"Query:\n{query?.ToString(Formatting.Indented)}\n");

            var uri = $"{_elasticSearchSettings.Host}/{indexName}/_search";

            JsonReader response = GetResponse(query, uri, out string rawJsonResult);

            var serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            TRoot rawResults = response == null
                ? default
                : serializer.Deserialize<TRoot>(response);

            return new RawResults<TRoot>
            {
                RootObject = rawResults,
                RawJson = rawJsonResult
            };
        }

        public async Task<TRoot> GetRawResultsAsync<TRoot>(RequestBase query, string language, CancellationToken cancellationToken, string indexName = null)
        {
            if (indexName == null)
                indexName = _elasticSearchSettings.GetDefaultIndexName(language);

            Logger.Information($"Index:\n{indexName}\n");
            Logger.Information($"Query:\n{query?.ToString(Formatting.Indented)}\n");

            var uri = $"{_elasticSearchSettings.Host}/{indexName}/_search";

            JsonReader response = await GetResponseAsync(query, uri, cancellationToken);

            var serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            return response == null
                ? default
                : serializer.Deserialize<TRoot>(response);
        }

        private void SetupFacets<T, TU>(EsRootObjectBase<T> results, SearchResultBase<TU> searchResult)
        {
            Dictionary<string, Aggregation> facets = results.Aggregations;
            if (facets?.Any() == true)
            {
                searchResult.Facets = facets.Select(f => new FacetEntry
                {
                    Key = f.Key,
                    Count = f.Value.Facets.Length,
                    Hits = f.Value.Facets.Select(i => new FacetHit
                    {
                        Key = i.Key,
                        Count = i.Count
                    }).ToArray()
                }).ToArray();
            }
        }

        public virtual string[] GetSuggestions(SuggestRequest request, CultureInfo culture, string indexName = null)
        {
            if (indexName == null)
                indexName = _elasticSearchSettings.GetDefaultIndexName(Language.GetLanguageCode(culture));

            var endpoint = $"{_elasticSearchSettings.Host}/{indexName}/_search";

            Logger.Information($"GetSuggestions query:\nGET {endpoint}\n{request?.ToString(Formatting.Indented)}\n");

            JsonReader response = GetResponse(request, endpoint, out _);

            var serializer = new JsonSerializer
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore
            };

            SuggestionsRootObject results = serializer.Deserialize<SuggestionsRootObject>(response);

            if (results?.Suggestions != null && results.Suggestions.Length > 0)
            {
                return results.Suggestions.SelectMany(s => s.Options.Select(o => o.Text)).ToArray();
            }

            return new string[0];
        }

        protected async Task<JsonReader> GetResponseAsync(RequestBase request, string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                var data = Encoding.UTF8.GetBytes(request.ToString());
                byte[] returnData = await HttpClientHelper.PostAsync(new Uri(endpoint), data, cancellationToken);
                if (returnData == null)
                    throw new Exception("Failed to POST to " + endpoint);

                string response = Encoding.UTF8.GetString(returnData);
                Logger.Debug("GetResponse response:\n" + JToken.Parse(response).ToString(Formatting.Indented));
                return new JsonTextReader(new StringReader(response));
            }
            catch (WebException ex)
            {
                TryLogErrors(ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not get response", ex);
            }

            return null;
        }

        public virtual JsonReader GetResponse(RequestBase request, string endpoint, out string rawJsonResult)
        {
            rawJsonResult = null;

            try
            {
                var data = Encoding.UTF8.GetBytes(request.ToString());
                byte[] returnData = HttpClientHelper.Post(new Uri(endpoint), data);
                if (returnData == null)
                    throw new Exception("Failed to POST to " + endpoint);

                string response = Encoding.UTF8.GetString(returnData);

                rawJsonResult = response;

                Logger.Debug("GetResponse response:\n" + JToken.Parse(response).ToString(Formatting.Indented));
                return new JsonTextReader(new StringReader(response));
            }
            catch (WebException ex)
            {
                TryLogErrors(ex);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not get response", ex);
            }

            return null;
        }

        private static void TryLogErrors(WebException webException)
        {
            // Assume the response is json
            try
            {
                using (var reader = new StreamReader(webException.Response.GetResponseStream()))
                {
                    Logger.Error(JToken.Parse(reader.ReadToEnd()).ToString(Formatting.Indented));
                }
            }
            catch
            {
                Logger.Error("Could not read error-response");
            }
        }
    }
}
