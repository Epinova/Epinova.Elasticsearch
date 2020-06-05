using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Epinova.ElasticSearch.Core.Attributes;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Core;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

namespace Epinova.ElasticSearch.Core.Engine
{
    /// <summary>
    /// For creating advanced queries
    /// </summary>
    internal class QueryBuilder
    {
        private static readonly ILogger _logger = LogManager.GetLogger(typeof(SearchEngine));
        private readonly Mapping _mapping;
        private const int BestBetMultiplier = 10000; //TODO: Expose in config?
        private readonly IBoostingRepository _boostingRepository;
        private readonly IElasticSearchSettings _settings;
        private static readonly string[] _excludedFields = { DefaultFields.Suggest };
        private string[] _mappedFields;

        private readonly string[] _searchableFieldTypes = {
            nameof(MappingType.Text).ToLower(),
            nameof(MappingType.Attachment).ToLower()
        };

        public QueryBuilder(
            IServerInfoService serverInfoService,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper)
        {
            _settings = settings;
            _mapping = new Mapping(serverInfoService, settings, httpClientHelper);
            ServiceLocator.Current?.TryGetExistingInstance(out _boostingRepository);
        }

        /// <summary>
        /// Used by tests
        /// </summary>
        internal void SetMappedFields(string[] fields) =>
            _mappedFields = fields;

        private string[] GetMappedFields(string language, string index, Type type)
        {
            _logger.Debug("Get mapped fields");

            if(_mappedFields?.Any() != true)
            {
                _logger.Debug("No mapped fields found, lookup with Mapping.GetIndexMapping");

                _mappedFields = _mapping.GetIndexMapping(type, language, index)
                    .Properties
                    .Where(m => _searchableFieldTypes.Contains(m.Value.Type)
                        && !m.Key.EndsWith(Models.Constants.KeywordSuffix))
                    .Select(m => m.Key)
                    .Except(_excludedFields)
                    .ToArray();
            }

            if(_logger.IsDebugEnabled())
            {
                _logger.Debug("Found:");
                _mappedFields.ToList().ForEach(_logger.Debug);
            }

            return _mappedFields;
        }

        /// <summary>
        /// A standard freetext search against all fields, constrained by type T. With facets.
        /// </summary>
        /// <param name="querySetup">The QuerySetup. <see cref="QuerySetup"/></param>
        public RequestBase TypedSearch<T>(QuerySetup querySetup) where T : class
        {
            querySetup.Type = typeof(T);
            return TypedSearch(querySetup);
        }

        public RequestBase TypedSearch(QuerySetup querySetup) => SearchInternal(querySetup);

        /// <summary>
        /// A standard freetext search against all fields
        /// </summary>
        /// <param name="querySetup">The QuerySetup. <see cref="QuerySetup"/></param>
        public RequestBase Search(QuerySetup querySetup)
            => SearchInternal(querySetup);

        internal RequestBase MoreLikeThis(QuerySetup setup)
            => new MoreLikeThisRequest(setup);

        private RequestBase SearchInternal(QuerySetup setup)
        {
            CheckSize(setup);

            var request = new QueryRequest(setup);

            if(setup.SearchFields.Count == 0)
            {
                setup.SearchFields.AddRange(GetMappedFields(Language.GetLanguageCode(setup.Language), setup.IndexName, setup.SearchType));
            }

            if(_logger.IsDebugEnabled())
            {
                _logger.Debug("SearchFields:");
                setup.SearchFields.ForEach(f => _logger.Debug(f));
            }

            SetupAttachmentFields(setup);
            SetupSourceFields(request, setup);

            if(setup.IsGetQuery)
            {
                request.Query.Bool.Must.Add(new MatchAll());
            }
            else if(setup.IsWildcard)
            {
                setup.SearchFields.ForEach(field =>
                    request.Query.Bool.Should.Add(new Wildcard(field, request.Query.SearchText)));

                // Boost hits that starts with searchtext, ie. when searching for "*foo*", 
                // hits on "foobar" will score higher than hits on "barfoo"
                if(request.Query.SearchText.StartsWith("*"))
                {
                    setup.SearchFields.ForEach(field =>
                        request.Query.Bool.Should.Add(new Wildcard(field, request.Query.SearchText.TrimStart('*'), 10)));
                }

                request.Query.Bool.MinimumNumberShouldMatch = 1;
            }
            else
            {
                request.Query.Bool.Must.Add(
                    new MatchMulti(
                        request.Query.SearchText,
                        setup.SearchFields,
                        setup.Operator,
                        null,
                        null,
                        setup.FuzzyLength,
                        setup.Analyzer));

                // Boost phrase matches if multiple words
                if(request.Query.SearchText?.IndexOf(" ", StringComparison.OrdinalIgnoreCase) > 0)
                {
                    request.Query.Bool.Should.Add(
                        new MatchMulti(
                            request.Query.SearchText,
                            setup.SearchFields,
                            setup.Operator,
                            "phrase",
                            2));
                }
            }

            SetupBoosting(setup, request);

            if(setup.FacetFieldNames.Count > 0)
            {
                request.Aggregation = GetAggregationQuery(setup.FacetFieldNames);
            }

            SetupFilters(setup, request);

            // Highlighting
            if(setup.UseHighlight)
            {
                request.Highlight = new Highlight
                {
                    Fields = GetHighlightFields()
                };
            }

            // Did-you-mean
            if(setup.EnableDidYouMean)
            {
                request.DidYouMeanSuggest = new DidYouMeanSuggest(request.Query.SearchText);
            }

            // function_score. Must be the last operation in this method.
            // Cannot use Gauss and ScriptScore simultaneously
            if(setup.ScriptScore != null && setup.Gauss.Count > 0)
            {
                throw new InvalidOperationException("Cannot use Gauss and ScriptScore simultaneously");
            }

            if(setup.ScriptScore != null)
            {
                request.Query = new FunctionScoreQuery(request.Query, setup.ScriptScore);
            }

            if(setup.Gauss.Count > 0)
            {
                request.Query = new FunctionScoreQuery(request.Query, setup.Gauss);
            }

            return request;
        }

        private void CheckSize(QuerySetup setup)
        {
            if(setup.From > 10000 || setup.Size > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(setup),
                    "From (skip) and size (take) must be less than or equal to 10000 according to default [index.max_result_window] setting.");
            }
        }

        private void SetupSourceFields(QueryRequest request, QuerySetup setup)
        {
            if(setup.SourceFields == null)
            {
                request.SourceFields = null;
                return;
            }

            var fields = setup.SourceFields.ToList();
            fields.AddRange(setup.SearchFields);

            fields.AddRange(Conventions.Indexing.CustomProperties.Select(c => c.Name));

            fields.Add(DefaultFields.Id);
            fields.Add(DefaultFields.Indexed);
            fields.Add(DefaultFields.ParentLink);
            fields.Add(DefaultFields.Name);
            fields.Add(DefaultFields.Type);
            fields.Add(DefaultFields.Types);
            fields.Add(DefaultFields.Path);
            fields.Add(DefaultFields.StartPublish);
            fields.Add(DefaultFields.StopPublish);
            fields.Add(DefaultFields.Created);
            fields.Add(DefaultFields.Changed);

            fields.Remove(DefaultFields.BestBets);
            fields.Remove(DefaultFields.AttachmentAuthor);
            fields.Remove(DefaultFields.AttachmentContent);
            fields.Remove(DefaultFields.AttachmentData);
            fields.Remove(DefaultFields.AttachmentKeywords);

            request.SourceFields = fields.Distinct().OrderBy(f => f).ToArray();
        }

        private static void AppendDefaultFilters(QueryBase query, Type type)
        {
            if(type.GetInheritancHierarchy().Contains(typeof(IVersionable)))
            {
                var stopPublishFilter = new Range<DateTime>(DefaultFields.StopPublish, true) { RangeSetting = { Gte = DateTime.Now } };
                query.Bool.Filter.Add(stopPublishFilter);
                var startPublishFilter = new Range<DateTime>(DefaultFields.StartPublish, true) { RangeSetting = { Lte = DateTime.Now } };
                query.Bool.Filter.Add(startPublishFilter);
            }
        }

        private void SetupBoosting(QuerySetup setup, QueryRequest request)
        {
            if(!setup.UseBoosting)
            {
                return;
            }

            List<Boost> boosting = GetBoosting(setup.Type, setup.BoostFields);
            if(boosting.Count > 0)
            {
                var searchText = request.Query.SearchText.Replace("*", String.Empty);
                if(!TextUtil.IsNumeric(searchText))
                {
                    boosting.RemoveAll(b => b.FieldName.Equals(DefaultFields.Id));
                }

                request.Query.Bool.Should.AddRange(
                    boosting.Select(b =>
                        new MatchWithBoost(b.FieldName,
                            searchText, b.Weight, setup.Operator)));
            }

            // Boosting by type
            if(setup.BoostTypes.Count > 0)
            {
                request.Query.Bool.Should.AddRange(
                    setup.BoostTypes.Select(b =>
                        new MatchWithBoost(DefaultFields.Types, b.Key.GetTypeName(), b.Value, setup.Operator)));

                // Direct match in Type gives higher score than match in Types, hence the +1
                request.Query.Bool.Should.AddRange(
                    setup.BoostTypes.Select(b =>
                        new MatchWithBoost(DefaultFields.Type, b.Key.GetTypeName(), b.Value + 1, setup.Operator)));
            }

            if(setup.BoostAncestors.Count > 0)
            {
                request.Query.Bool.Should.AddRange(
                    setup.BoostAncestors.Select(b =>
                        new MatchWithBoost(DefaultFields.Path, b.Key.ToString(), b.Value, setup.Operator)));

                request.Query.Bool.Should.AddRange(
                    setup.BoostAncestors.Select(b =>
                        new MatchWithBoost(DefaultFields.Id, b.Key.ToString(), b.Value, setup.Operator)));
            }

            // Best Bets
            if(setup.UseBestBets && !String.IsNullOrWhiteSpace(request.Query.SearchText))
            {
                IEnumerable<string> terms = request.Query.SearchText
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim().Trim('*'));

                var key = setup.IndexName ?? _settings.GetDefaultIndexName(Language.GetLanguageCode(setup.Language));

                if(!Conventions.Indexing.BestBets.TryGetValue(key, out List<BestBet> bestBetsForLanguage))
                {
                    return;
                }

                IEnumerable<BestBet> bestBets = bestBetsForLanguage
                    .Where(b => b.GetTerms().Any(t => terms.Contains(t)));

                request.Query.Bool.Should.AddRange(
                    bestBets.Select(_ =>
                        new MatchWithBoost(
                            DefaultFields.BestBets, request.Query.SearchText.Trim('*'), BestBetMultiplier, setup.Operator)));
            }
        }

        private static void SetupFilters(QuerySetup setup, QueryRequest request)
        {
            var filterQuery = new NestedBoolQuery();

            // Filter away excluded types
            if(setup.ExcludedTypes.Count > 0)
            {
                filterQuery.Bool.MustNot.AddRange(setup.ExcludedTypes.Select(e => new MatchSimple(DefaultFields.Types, e.GetTypeName().ToLower())));
            }

            foreach(var ex in GetExcludedRoots(setup))
            {
                filterQuery.Bool.MustNot.Add(new MatchSimple(DefaultFields.Id, ex.Key.ToString()));

                if(ex.Value)
                {
                    filterQuery.Bool.MustNot.Add(new MatchSimple(DefaultFields.Path, ex.Key.ToString()));
                }
            }

            // Filter on type
            if(setup.Type != null)
            {
                Term term = CreateTerm(new Filter(DefaultFields.Types, setup.Type.GetTypeName().ToLower(), null, false, Operator.And));
                filterQuery.Bool.Must.Add(term);
            }

            // Filter on ranges
            if(setup.Ranges.Count > 0)
            {
                request.Query.Bool.Filter.AddRange(setup.Ranges);
            }

            // Filter on root-id
            if(setup.RootId != 0)
            {
                var term = new Term(DefaultFields.Path, Convert.ToString(setup.RootId), true);
                filterQuery.Bool.Must.Add(term);
            }

            // Filter on ACL
            if(setup.AppendAclFilters && setup.AclPrincipal != null)
            {
                var boolQuery = new NestedBoolQuery();

                foreach(var role in setup.AclPrincipal.RoleList)
                {
                    var roleTerm = new MatchSimple(DefaultFields.Acl, $"R:{role}");
                    boolQuery.Bool.Should.Add(roleTerm);
                }

                var userTerm = new MatchSimple(DefaultFields.Acl, $"U:{setup.AclPrincipal.Name}");
                boolQuery.Bool.Should.Add(userTerm);

                boolQuery.Bool.MinimumNumberShouldMatch = 1;

                request.Query.Bool.Filter.Add(boolQuery);
            }

            if(setup.FilterGroups.Count > 0)
            {
                foreach(var filterGroup in setup.FilterGroups)
                {
                    foreach(var filter in filterGroup.Value.Filters)
                    {
                        var boolQuery = new BoolQuery();
                        if(filter.Operator == Operator.Or)
                        {
                            boolQuery.MinimumNumberShouldMatch = 1;
                            boolQuery.Should = new List<MatchBase>();

                            if(ArrayHelper.IsArrayCandidate(filter.Value.GetType()))
                            {
                                foreach(var value in (IEnumerable)ArrayHelper.ToArray(filter.Value))
                                {
                                    boolQuery.Should.Add(Term.FromFilter(filter, value));
                                }
                            }
                            else
                            {
                                boolQuery.Should.Add(Term.FromFilter(filter));
                            }
                        }
                        else if(filter.Operator == Operator.And)
                        {
                            boolQuery.Must = new List<MatchBase>();

                            if(ArrayHelper.IsArrayCandidate(filter.Value.GetType()))
                            {
                                foreach(var value in (IEnumerable)ArrayHelper.ToArray(filter.Value))
                                {
                                    boolQuery.Must.Add(Term.FromFilter(filter, value));
                                }
                            }
                            else
                            {
                                boolQuery.Must.Add(Term.FromFilter(filter));
                            }
                        }

                        // Use regular or post filter?
                        if(!setup.UsePostfilters)
                        {
                            request.Query.Bool.Filter.Add(Term.FromArrayFilter(filter));
                        }
                        else
                        {
                            if(filterGroup.Value.Operator == Operator.And)
                            {
                                request.PostFilter.Bool.Must.Add(new NestedBoolQuery(boolQuery));
                            }
                            else if(filterGroup.Value.Operator == Operator.Or)
                            {
                                request.PostFilter.Bool.Should.Add(new NestedBoolQuery(boolQuery));
                            }
                        }
                    }
                }
            }

            if(setup.Filters.Count > 0)
            {
                // Add not-filters as regular filter regardless of post-filter value
                IEnumerable<Filter> notFilters = setup.Filters.Where(f => f.Not).ToArray();
                filterQuery.Bool.MustNot.AddRange(notFilters.Select(CreateTerm));

                // GeoFilters
                IEnumerable<Filter> geoFilters = setup.Filters.Where(f => f.Type == typeof(GeoPoint)).ToArray();

                // Use regular or post filter?
                if(!setup.UsePostfilters)
                {
                    request.Query.Bool.Filter.AddRange(
                        setup.Filters
                            .Except(notFilters)
                            .Except(geoFilters)
                            .Select(CreateTerm));

                    request.Query.Bool.Filter.AddRange(
                        geoFilters.Select(g => g.Value as MatchBase));
                }
                else
                {
                    request.PostFilter.Bool.Must.AddRange(
                        setup.Filters
                          .Except(notFilters)
                          .Except(geoFilters)
                          .Where(f => f.Operator == Operator.And && !f.Not)
                          .Select(CreateTerm));

                    request.PostFilter.Bool.Must.AddRange(
                        geoFilters.Select(g => g.Value as MatchBase));

                    request.PostFilter.Bool.Should.AddRange(
                        setup.Filters
                          .Except(notFilters)
                          .Except(geoFilters)
                          .Where(f => f.Operator == Operator.Or && !f.Not)
                          .Select(CreateTerm));
                }
            }

            if(filterQuery.HasAnyValues())
            {
                request.Query.Bool.Filter.Add(filterQuery);
            }

            if(setup.ApplyDefaultFilters)
            {
                AppendDefaultFilters(request.Query, setup.Type);
            }

            if(request.Query.Bool.Should.Count > 1 && request.Query.Bool.Must.Count == 0)
            {
                request.Query.Bool.MinimumNumberShouldMatch = 1;
            }
            else
            {
                request.Query.Bool.MinimumNumberShouldMatch = null;
            }

            if(request.PostFilter.Bool.Should.Count > 0 && request.PostFilter.Bool.Must.Count == 0)
            {
                request.PostFilter.Bool.MinimumNumberShouldMatch = 1;
            }
            else
            {
                request.PostFilter.Bool.MinimumNumberShouldMatch = null;
            }
        }

        private static Dictionary<int, bool> GetExcludedRoots(QuerySetup setup)
        {
            Dictionary<int, bool> excludedRoots = setup.ExcludedRoots;
            Conventions.Indexing.ExcludedRoots.ToList().ForEach(x =>
                excludedRoots.Add(x, true));
            return excludedRoots;
        }

        private static Term CreateTerm(Filter filter)
        {
            return ArrayHelper.IsArrayCandidate(filter.Value.GetType())
                ? Term.FromArrayFilter(filter)
                : Term.FromFilter(filter);
        }

        private static Dictionary<string, object> GetHighlightFields()
        {
            object settings = new { fragment_size = Conventions.Indexing.HighlightFragmentSize };

            var fields = WellKnownProperties.Analyze.ToDictionary(x => x, _ => settings);

            Conventions.Indexing.Highlights.ToList().ForEach(h =>
            {
                if(!fields.ContainsKey(h))
                {
                    fields.Add(h, settings);
                }
            });

            return fields;
        }

        private static void SetupAttachmentFields(QuerySetup querySetup)
        {
            querySetup.SearchFields.Remove(DefaultFields.AttachmentData);
            querySetup.SearchFields.Remove(DefaultFields.AttachmentContent);
            querySetup.SearchFields.Remove(DefaultFields.AttachmentAuthor);
            querySetup.SearchFields.Remove(DefaultFields.AttachmentKeywords);

            querySetup.SearchFields.Add(DefaultFields.AttachmentContent);
            querySetup.SearchFields.Add(DefaultFields.AttachmentAuthor);
            querySetup.SearchFields.Add(DefaultFields.AttachmentKeywords);
        }

        private static Dictionary<string, Bucket> GetAggregationQuery(Dictionary<string, MappingType> fields)
        {
            var sortedFields = fields.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value);

            if(sortedFields.Count == 0)
            {
                return null;
            }

            var aggregations = new Dictionary<string, Bucket>();

            foreach(KeyValuePair<string, MappingType> field in sortedFields)
            {
                var key = field.Key;
                if(field.Value == MappingType.Text && !key.EndsWith(Models.Constants.KeywordSuffix))
                {
                    key = String.Concat(key, Models.Constants.KeywordSuffix);
                }

                aggregations.Add(key, new Bucket(key));
            }

            return aggregations;
        }

        internal List<Boost> GetBoosting(Type type, Dictionary<string, byte> boostFields)
        {
            var boost = boostFields
                .Select(f => new Boost
                {
                    FieldName = f.Key,
                    Weight = f.Value
                })
                .ToList();

            if(type == null)
            {
                return boost;
            }

            var boostingFromAttributes = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                .Where(p => p.GetCustomAttributes(typeof(BoostAttribute), true).Length > 0)
                .Select(p => new Boost
                {
                    FieldName = p.Name,
                    Weight = p.GetCustomAttribute<BoostAttribute>().Weight
                })
                .Where(p => boost.All(f => f.FieldName != p.FieldName))
                .ToList();

            boost.AddRange(boostingFromAttributes);

            if(_boostingRepository == null)
            {
                return boost;
            }

            var boostingFromDb = _boostingRepository.GetByType(type)
                .Select(p => new Boost
                {
                    FieldName = p.Key,
                    Weight = p.Value
                })
                .ToList();

            // Editorial entries has presedence
            foreach(Boost dbBoost in boostingFromDb)
            {
                if(boost.Any(b => b.FieldName == dbBoost.FieldName))
                {
                    boost.First(b => b.FieldName == dbBoost.FieldName).Weight =
                        Math.Max(boost.First(b => b.FieldName == dbBoost.FieldName).Weight, dbBoost.Weight);

                    _logger.Debug(
                        $"Overriding boost weight from becuase of editorial entry. Old: {boost.First(b => b.FieldName == dbBoost.FieldName).Weight}. New: {dbBoost.Weight}");
                }
            }

            boostingFromDb = boostingFromDb.Where(p => boost.All(f => f.FieldName != p.FieldName)).ToList();

            boost.AddRange(boostingFromDb);

            return boost;
        }
    }
}
