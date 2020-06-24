using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Engine;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Logging;
using EPiServer.Security;

#pragma warning disable 693
namespace Epinova.ElasticSearch.Core
{
    public partial class ElasticSearchService<T> : IElasticSearchService<T>
    {
        private readonly QueryBuilder _builder;
        private readonly SearchEngine _engine;
        private readonly Dictionary<string, MappingType> _facetFields;
        private readonly List<Type> _excludedTypes;
        private readonly ILogger _logger = LogManager.GetLogger(typeof(ElasticSearchService));
        private readonly Dictionary<Type, sbyte> _boostTypes;
        internal readonly Dictionary<int, bool> ExcludedRoots;
        internal readonly Dictionary<string, byte> BoostFields;
        internal readonly Dictionary<int, sbyte> BoostAncestors;
        internal List<Filter> PostFilters { get; }
        internal Dictionary<string, FilterGroupQuery> PostFilterGroups { get; }
        protected internal readonly List<string> SearchFields;
        internal readonly List<Sort> SortFields;
        private string _fuzzyLength;
        private bool _usePostfilters;
        private bool _appendAclFilters;

        // function_score values
        private readonly List<Gauss> _gauss;
        private string _customScriptScoreSource;
        private object _customScriptScoreParams;
        private string _customScriptScoreLanguage;

        public int RootId { get; private set; }
        public bool TrackSearch { get; private set; }
        public bool EnableBestBets { get; private set; }
        public bool EnableHighlight { get; private set; }
        public Type SearchType { get; set; }
        public Type Type { get; private set; }
        public bool IsWildcard { get; private set; }
        public bool IsGetQuery { get; private set; }
        public Operator Operator { get; private set; }
        public string Analyzer { get; private set; }
        public string SearchText { get; private set; }
        public string MoreLikeId { get; private set; }
        public int MltMinTermFreq { get; private set; }
        public int MltMinDocFreq { get; private set; }
        public int MltMinWordLength { get; private set; }
        public int MltMaxQueryTerms { get; set; }
        public bool UseBoosting { get; private set; }
        public int FromValue { get; private set; }
        public int SizeValue { get; private set; }
        public CultureInfo SearchLanguage { get; private set; }

        private string _indexName;
        private PrincipalInfo _aclPrincipal;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly IServerInfoService _serverInfoService;
        private readonly IElasticSearchSettings _settings;

        public string IndexName
        {
            get => _indexName?.ToLower();
            private set => _indexName = value;
        }

        public CultureInfo CurrentLanguage => SearchLanguage;

        internal ElasticSearchService(
            IServerInfoService serverInfoService,
            IElasticSearchSettings settings,
            IHttpClientHelper httpClientHelper)
        {
            BoostFields = new Dictionary<string, byte>();
            BoostAncestors = new Dictionary<int, sbyte>();
            ExcludedRoots = new Dictionary<int, bool>();
            SortFields = new List<Sort>();
            SearchFields = new List<string>();
            SearchLanguage = CultureInfo.CurrentCulture;
            SearchType = typeof(IndexItem);
            SizeValue = 10;
            PostFilters = new List<Filter>();
            PostFilterGroups = new Dictionary<string, FilterGroupQuery>();
            UseBoosting = true;
            _boostTypes = new Dictionary<Type, sbyte>();
            _builder = new QueryBuilder(serverInfoService, settings, httpClientHelper);
            _engine = new SearchEngine(serverInfoService, settings, httpClientHelper);
            _excludedTypes = new List<Type>();
            _facetFields = new Dictionary<string, MappingType>();
            _gauss = new List<Gauss>();
            _httpClientHelper = httpClientHelper;
            _ranges = new List<RangeBase>();
            _serverInfoService = serverInfoService;
            _settings = settings;
            _usePostfilters = true;
        }

        public IElasticSearchService<T> Boost(Expression<Func<T, object>> fieldSelector, byte weight)
        {
            var fieldName = GetFieldName(fieldSelector);

            if(BoostFields.ContainsKey(fieldName) || weight <= Byte.MinValue)
            {
                return this;
            }

            _logger.Debug($"Boosting field: '{fieldName} ({weight})'");
            BoostFields.Add(fieldName, weight);

            return this;
        }

        public IElasticSearchService<T> Boost<TBoost>(sbyte weight)
            => Boost(typeof(TBoost), weight);

        public IElasticSearchService<T> Boost(Type type, sbyte weight)
        {
            if(!_boostTypes.ContainsKey(type))
            {
                _logger.Debug($"Boosting type: '{type.FullName} ({weight})'");
                _boostTypes.Add(type, weight);
            }

            return this;
        }

        public IElasticSearchService<T> BoostByAncestor(int path, sbyte weight)
        {
            if(!BoostAncestors.ContainsKey(path))
            {
                _logger.Debug($"Boosting by ancestor: '{path} ({weight})'");
                BoostAncestors.Add(path, weight);
            }

            return this;
        }

        public IElasticSearchService<T> Decay(Expression<Func<T, DateTime?>> fieldSelector, TimeSpan scale = default, TimeSpan offset = default)
            => Decay(GetFieldName(fieldSelector), scale, offset);

        public IElasticSearchService<T> Decay(string fieldName, TimeSpan scale = default, TimeSpan offset = default)
        {
            if(scale == default)
            {
                scale = TimeSpan.FromDays(30);
            }

            if(_gauss.Any(g => fieldName.Equals(g.Field, StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new InvalidOperationException($"Decay for '{fieldName}' already defined");
            }

            _gauss.Add(new Gauss
            {
                Field = fieldName,
                Scale = scale.TotalSeconds.ToString(CultureInfo.InvariantCulture) + "s",
                Offset = offset.TotalSeconds.ToString(CultureInfo.InvariantCulture) + "s"
            });

            return this;
        }

        public IElasticSearchService<T> CustomScriptScore(string script, string scriptLanguage = null, object parameters = null)
        {
            if(!String.IsNullOrEmpty(script))
            {
                _customScriptScoreSource = script;
                _customScriptScoreParams = parameters;
                _customScriptScoreLanguage = scriptLanguage ?? "painless";
            }

            return this;
        }

        public IElasticSearchService<T> UseIndex(string index)
        {
            IndexName = index;
            return this;
        }

        public IElasticSearchService<T> Fuzzy(byte? length = null)
        {
            _fuzzyLength = length?.ToString() ?? "AUTO";

            return this;
        }

        public IElasticSearchService<T> Exclude<TType>()
            => Exclude(typeof(TType));

        public IElasticSearchService<T> Exclude(Type type)
        {
            if(!_excludedTypes.Contains(type))
            {
                _excludedTypes.Add(type);
            }

            return this;
        }

        public IElasticSearchService<T> Exclude(int rootId, bool recursive = true)
        {
            if(!ExcludedRoots.ContainsKey(rootId))
            {
                ExcludedRoots.Add(rootId, recursive);
            }

            return this;
        }

        public IElasticSearchService<T> From(int from)
        {
            FromValue = from;

            return this;
        }

        public IElasticSearchService<T> Language(CultureInfo language)
        {
            SearchLanguage = language;

            return this;
        }

        public IElasticSearchService<T> Skip(int skip)
            => From(skip);

        public IElasticSearchService<T> Size(int size)
        {
            SizeValue = size;

            return this;
        }

        public IElasticSearchService<T> Take(int take)
            => Size(take);

        public IElasticSearchService<T> NoBoosting()
        {
            UseBoosting = false;

            return this;
        }

        public IElasticSearchService<T> Get<T>()
        {
            return new ElasticSearchService<T>(_serverInfoService, _settings, _httpClientHelper)
            {
                Type = typeof(T),
                SearchLanguage = SearchLanguage,
                RootId = RootId,
                SearchType = SearchType,
                UseBoosting = UseBoosting,
                EnableBestBets = EnableBestBets,
                FromValue = FromValue,
                SizeValue = SizeValue,
                IsGetQuery = true,
                SearchText = String.Empty,
                IndexName = IndexName
            };
        }

        public IElasticSearchService<object> Search(string searchText, Operator @operator = Operator.Or)
            => Search<object>(searchText, null, @operator);

        public IElasticSearchService<T> Search<T>(string searchText, Operator @operator = Operator.Or)
            => Search<T>(searchText, null, @operator);

        public IElasticSearchService<T> Search<T>(string searchText, string facetFieldName,
            Operator @operator = Operator.Or)
        {
            return new ElasticSearchService<T>(_serverInfoService, _settings, _httpClientHelper)
            {
                Type = typeof(T),
                SearchText = searchText,
                Operator = @operator,
                SearchLanguage = SearchLanguage,
                RootId = RootId,
                SearchType = SearchType,
                UseBoosting = UseBoosting,
                EnableBestBets = EnableBestBets,
                FromValue = FromValue,
                SizeValue = SizeValue,
                IsWildcard = IsWildcard,
                TrackSearch = TrackSearch,
                IsGetQuery = IsGetQuery,
                IndexName = IndexName
            };
        }

        public SearchResult GetResults(bool enableHighlighting = true, bool enableDidYouMean = true, bool applyDefaultFilters = true, params string[] fields)
        {
            QuerySetup query = CreateQuery(applyDefaultFilters, fields);
            query.EnableDidYouMean = enableDidYouMean;
            query.EnableHighlighting = enableHighlighting;

            return GetResults(query);
        }

        public async Task<SearchResult> GetResultsAsync(bool enableHighlighting = true, bool enableDidYouMean = true, bool applyDefaultFilters = true, params string[] fields)
            => await GetResultsAsync(CancellationToken.None, enableHighlighting, enableDidYouMean, applyDefaultFilters, fields)
                        .ConfigureAwait(false);

        public async Task<SearchResult> GetResultsAsync(CancellationToken cancellationToken, bool enableHighlighting = true, bool enableDidYouMean = true, bool applyDefaultFilters = true, params string[] fields)
        {
            QuerySetup query = CreateQuery(applyDefaultFilters, fields);
            query.EnableDidYouMean = enableDidYouMean;
            query.EnableHighlighting = enableHighlighting;

            return await GetResultsAsync(query, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CustomSearchResult<T>> GetCustomResultsAsync()
            => await GetCustomResultsAsync(CancellationToken.None).ConfigureAwait(false);

        public async Task<CustomSearchResult<T>> GetCustomResultsAsync(CancellationToken cancellationToken)
        {
            QuerySetup query = CreateQuery(true);
            query.EnableDidYouMean = false;

            // Always return all fields for custom objects
            query.SourceFields = null;

            return await GetCustomResultsAsync<T>(query, cancellationToken).ConfigureAwait(false);
        }

        public CustomSearchResult<T> GetCustomResults()
        {
            QuerySetup query = CreateQuery(false);

            query.SearchType = typeof(T);

            // Always return all fields for custom objects
            query.SourceFields = null;

            return GetCustomResults<T>(query);
        }

        private QuerySetup CreateQuery(bool applyDefaultFilters, params string[] fields)
        {
            var serverVersion = _serverInfoService.GetInfo().Version;

            return new QuerySetup
            {
                ApplyDefaultFilters = applyDefaultFilters,
                Analyzer = Analyzer,
                BoostAncestors = BoostAncestors,
                BoostFields = BoostFields,
                BoostTypes = _boostTypes,
                FuzzyLength = _fuzzyLength,
                UsePostfilters = _usePostfilters,
                AppendAclFilters = _appendAclFilters,
                AclPrincipal = _aclPrincipal,
                Gauss = _gauss,
                ScriptScore = CreateScriptScore(serverVersion),
                ServerVersion = serverVersion,
                Type = Type,
                MoreLikeId = MoreLikeId,
                MltMinTermFreq = MltMinTermFreq,
                MltMinDocFreq = MltMinDocFreq,
                MltMinWordLength = MltMinWordLength,
                MltMaxQueryTerms = MltMaxQueryTerms,
                ExcludedTypes = _excludedTypes,
                ExcludedRoots = ExcludedRoots,
                Language = SearchLanguage,
                SearchFields = SearchFields,
                SearchText = SearchText,
                FacetFieldNames = _facetFields,
                Filters = PostFilters,
                FilterGroups = PostFilterGroups,
                Ranges = _ranges,
                Operator = Operator,
                UseBoosting = UseBoosting,
                From = FromValue,
                Size = SizeValue,
                RootId = RootId,
                IsWildcard = IsWildcard,
                IsGetQuery = IsGetQuery,
                SourceFields = fields,
                SearchType = SearchType,
                SortFields = SortFields,
                UseBestBets = EnableBestBets,
                UseHighlight = EnableHighlight,
                IndexName = IndexName
            };
        }

        private ScriptScore CreateScriptScore(Version version)
        {
            if(String.IsNullOrEmpty(_customScriptScoreSource))
            {
                return null;
            }

            var scriptScore = new ScriptScore
            {
                Script = new ScriptScore.ScriptScoreInner
                {
                    Language = _customScriptScoreLanguage,
                    Parameters = _customScriptScoreParams
                }
            };

            if(version >= Constants.InlineVsSourceVersion)
            {
                scriptScore.Script.Source = _customScriptScoreSource;
            }
            else
            {
                scriptScore.Script.Inline = _customScriptScoreSource;
            }

            return scriptScore;
        }

        public IElasticSearchService<T> MoreLikeThis<T>(string id, int minimumTermFrequency = 1, int maxQueryTerms = 25, int minimumDocFrequency = 3, int minimumWordLength = 3)
        {
            return new ElasticSearchService<T>(_serverInfoService, _settings, _httpClientHelper)
            {
                MoreLikeId = id,
                MltMinTermFreq = minimumTermFrequency,
                MltMinDocFreq = minimumDocFrequency,
                MltMinWordLength = minimumWordLength,
                MltMaxQueryTerms = maxQueryTerms,
                Type = typeof(T),
                SearchLanguage = SearchLanguage,
                RootId = RootId,
                SearchType = SearchType,
                UseBoosting = false,
                EnableBestBets = false,
                FromValue = FromValue,
                SizeValue = SizeValue,
                IsWildcard = false,
                IndexName = IndexName
            };
        }

        public IElasticSearchService<object> WildcardSearch(string searchText)
            => WildcardSearch<object>(searchText);

        public IElasticSearchService<T> WildcardSearch<T>(string searchText)
        {
            return new ElasticSearchService<T>(_serverInfoService, _settings, _httpClientHelper)
            {
                Type = typeof(T),
                SearchText = searchText,
                Operator = Operator,
                SearchLanguage = SearchLanguage,
                RootId = RootId,
                SearchType = SearchType,
                UseBoosting = UseBoosting,
                EnableBestBets = EnableBestBets,
                FromValue = FromValue,
                SizeValue = SizeValue,
                IsWildcard = true,
                IndexName = IndexName
            };
        }

        public IElasticSearchService<T> StartFrom(int id)
        {
            RootId = id;
            return this;
        }

        public IElasticSearchService<T> SortBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector)
            => Sort(fieldSelector, false, true, default);

        public IElasticSearchService<T> SortBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min")
            where TProperty : GeoPoint
            => Sort(fieldSelector, false, true, compareTo, unit, mode);

        public IElasticSearchService<T> SortByScript(string script, bool descending, string type = "string", object parameters = null, string scriptLanguage = null)
        {
            scriptLanguage = scriptLanguage ?? "painless";

            if(String.IsNullOrWhiteSpace(script))
            {
                throw new InvalidOperationException("Script cannot be empty");
            }

            if(type != "string" && type != "number")
            {
                throw new InvalidOperationException("Type must be either 'string' or 'number'");
            }

            SortFields.Add(new ScriptSort
            {
                Direction = descending ? "desc" : "asc",
                Type = type,
                Script = script,
                Parameters = parameters,
                Language = scriptLanguage
            });

            return this;
        }

        public IElasticSearchService<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector)
            => Sort(fieldSelector, false, false);

        public IElasticSearchService<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min")
            where TProperty : GeoPoint
            => Sort(fieldSelector, false, false, compareTo, unit, mode);

        public IElasticSearchService<T> SortByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector)
            => Sort(fieldSelector, true, true);

        public IElasticSearchService<T> SortByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min")
            where TProperty : GeoPoint
            => Sort(fieldSelector, false, true, compareTo, unit, mode);

        public IElasticSearchService<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector)
            => Sort(fieldSelector, true, false);

        public IElasticSearchService<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min")
            where TProperty : GeoPoint
            => Sort(fieldSelector, false, false, compareTo, unit, mode);

        private IElasticSearchService<T> Sort<TProperty>(
            Expression<Func<T, TProperty>> fieldSelector,
            bool descending,
            bool primary,
            (double Lat, double Lon) compareTo = default,
            string unit = "km",
            string mode = "min")
        {
            if(primary && SortFields.Count > 0)
            {
                throw new InvalidOperationException("Query is already sorted. Use ThenBy or ThenByDescending to apply further sorting");
            }

            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            if(typeof(TProperty) == typeof(GeoPoint))
            {
                SortFields.Add(new GeoSort
                {
                    FieldName = fieldInfo.Item1,
                    Direction = descending ? "desc" : "asc",
                    MappingType = fieldInfo.Item2,
                    Unit = unit,
                    Mode = mode,
                    CompareTo = compareTo
                });
            }
            else
            {
                SortFields.Add(new Sort
                {
                    FieldName = fieldInfo.Item1,
                    Direction = descending ? "desc" : "asc",
                    MappingType = fieldInfo.Item2
                });
            }

            return this;
        }

        public IElasticSearchService<T> InField(Expression<Func<T, object>> fieldSelector, bool boost)
        {
            Boost(fieldSelector, Byte.MaxValue);
            return InField(fieldSelector);
        }

        public IElasticSearchService<T> InField(Expression<Func<T, object>> fieldSelector)
        {
            var fieldName = GetFieldName(fieldSelector);

            _logger.Debug($"Adding field: '{fieldName}'");

            if(!String.IsNullOrWhiteSpace(fieldName))
            {
                if(SearchFields.Contains(fieldName))
                {
                    throw new InvalidOperationException($"Field {fieldName} already added to query");
                }

                SearchFields.Add(fieldName);
            }

            return this;
        }

        public IElasticSearchService<T> FacetsFor(Expression<Func<T, object>> fieldSelector, bool usePostFilter = true, Type explicitType = null)
        {
            _usePostfilters = usePostFilter;
            var fieldInfo = GetFieldInfo(fieldSelector, explicitType);

            if(!String.IsNullOrWhiteSpace(fieldInfo.Item1) && !_facetFields.ContainsKey(fieldInfo.Item1))
            {
                _facetFields.Add(fieldInfo.Item1, fieldInfo.Item2);
            }

            return this;
        }

        public IElasticSearchService<T> FilterByACL(PrincipalInfo principal = null)
        {
            _appendAclFilters = true;
            _aclPrincipal = principal ?? PrincipalInfo.Current;

            return this;
        }

        public IElasticSearchService<T> Filter<TType>(string fieldName, TType filterValue, bool raw = true, Operator @operator = Operator.And)
        {
            if(filterValue != null)
            {
                PostFilters.Add(new Filter(fieldName, filterValue, typeof(TType), raw, @operator));
            }

            return this;
        }

        public IElasticSearchService<T> Filter<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue, bool raw = true, Operator @operator = Operator.And)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return Filter(fieldInfo.Item1, filterValue, raw, @operator);
        }

        public IElasticSearchService<T> Filter<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return Filter(fieldInfo.Item1, filterValue, raw);
        }

        public IElasticSearchService<T> Filter<TType>(Expression<Action<T>> fieldSelector, TType filterValue, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return Filter(fieldInfo.Item1, filterValue, raw);
        }

        public IElasticSearchService<T> Filters<TType>(Expression<Func<T, TType>> fieldSelector, IEnumerable<TType> filterValues, Operator @operator = Operator.Or, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return Filters(fieldInfo.Item1, filterValues, @operator, raw);
        }

        public IElasticSearchService<T> Filters<TType>(string fieldName, IEnumerable<TType> filterValues, Operator @operator = Operator.Or, bool raw = true)
        {
            TType[] values = filterValues as TType[] ?? filterValues.ToArray();
            if(values.Length > 0)
            {
                foreach(TType value in values)
                {
                    PostFilters.Add(new Filter(fieldName, value, typeof(TType), raw, @operator));
                }
            }

            return this;
        }

        public IElasticSearchService<T> Filters<TType>(Expression<Action<T>> fieldSelector, IEnumerable<TType> filterValues, Operator @operator = Operator.Or, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return Filters(fieldInfo.Item1, filterValues, @operator, raw);
        }

        public IElasticSearchService<T> FilterMustNot<TType>(string fieldName, TType filterValue, bool raw = true)
        {
            if(filterValue != null)
            {
                PostFilters.Add(new Filter(fieldName, filterValue, typeof(TType), raw, Operator.And, true));
            }

            return this;
        }

        public IElasticSearchService<T> FilterMustNot<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return FilterMustNot(fieldInfo.Item1, filterValue, raw);
        }

        public IElasticSearchService<T> FilterMustNot<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return FilterMustNot(fieldInfo.Item1, filterValue, raw);
        }

        public IElasticSearchService<T> FilterMustNot<TType>(Expression<Action<T>> fieldSelector, TType filterValue, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return FilterMustNot(fieldInfo.Item1, filterValue, raw);
        }

        public IElasticSearchService<T> FiltersMustNot<TType>(string fieldName, IEnumerable<TType> filterValues, bool raw = true)
        {
            TType[] values = filterValues as TType[] ?? filterValues.ToArray();
            if(values.Length > 0)
            {
                foreach(TType value in values)
                {
                    PostFilters.Add(new Filter(fieldName, value, typeof(TType), raw, Operator.And, true));
                }
            }

            return this;
        }

        public IElasticSearchService<T> FiltersMustNot<TType>(Expression<Func<T, TType>> fieldSelector, IEnumerable<TType> filterValues, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return FiltersMustNot(fieldInfo.Item1, filterValues, raw);
        }

        public IElasticSearchService<T> FiltersMustNot<TType>(Expression<Action<T>> fieldSelector, IEnumerable<TType> filterValues, bool raw = true)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);

            return FiltersMustNot(fieldInfo.Item1, filterValues, raw);
        }

        public IElasticSearchService<T> FilterGroup(Expression<Func<IFilterGroup<T>, IFilterGroup<T>>> groupExpression)
        {
            if(groupExpression.Body is MethodCallExpression expression)
            {
                groupExpression.Compile().Invoke(new FilterGroup<T>(this, Guid.NewGuid().ToString()));
            }

            return this;
        }

        public IElasticSearchService<T> Track()
        {
            TrackSearch = true;
            return this;
        }

        public IElasticSearchService<T> SetAnalyzer(string analyzer)
        {
            Analyzer = analyzer;
            return this;
        }

        public IElasticSearchService<T> UseBestBets()
        {
            EnableBestBets = true;
            return this;
        }

        public IElasticSearchService<T> Highlight()
        {
            EnableHighlight = true;
            return this;
        }

        internal async Task<SearchResult> GetResultsAsync(QuerySetup querySetup, CancellationToken cancellationToken)
        {
            RequestBase request = _builder.TypedSearch(querySetup);
            return await _engine.QueryAsync(request, querySetup.Language, cancellationToken, IndexName).ConfigureAwait(false);
        }

        internal SearchResult GetResults(QuerySetup querySetup)
        {
            RequestBase request = querySetup.MoreLikeId != null
                ? _builder.MoreLikeThis(querySetup)
                : _builder.TypedSearch(querySetup);

            return _engine.Query(request, querySetup.Language, IndexName);
        }

        internal async Task<CustomSearchResult<T>> GetCustomResultsAsync<T>(QuerySetup querySetup, CancellationToken cancellationToken)
        {
            RequestBase request = _builder.TypedSearch(querySetup);
            return await _engine.CustomQueryAsync<T>(request, querySetup.Language, cancellationToken, IndexName).ConfigureAwait(false);
        }

        internal CustomSearchResult<T> GetCustomResults<T>(QuerySetup querySetup)
        {
            RequestBase request = _builder.TypedSearch(querySetup);
            return _engine.CustomQuery<T>(request, querySetup.Language, IndexName);
        }

        internal static Tuple<string, MappingType> GetFieldInfo(Expression<Action<T>> fieldSelector, Type explicitType = null)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfoFromExpression(fieldSelector.Body, explicitType);

            return fieldInfo ?? new Tuple<string, MappingType>(fieldSelector.ToString(), MappingType.Text);
        }

        internal static Tuple<string, MappingType> GetFieldInfo<TProperty>(Expression<Func<T, TProperty>> fieldSelector, Type explicitType = null)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfoFromExpression(fieldSelector.Body, explicitType);

            return fieldInfo ?? new Tuple<string, MappingType>(fieldSelector.ToString(), MappingType.Text);
        }

        private static Tuple<string, MappingType> GetFieldInfoFromExpression(Expression expression, Type explicitType)
        {
            string fieldName;
            MappingType fieldType;

            switch(expression)
            {
                case MethodCallExpression callExpression:
                    {
                        MethodInfo methodInfo = callExpression.Method;

                        if(ArrayHelper.IsArrayCandidate(methodInfo.ReturnType))
                        {
                            explicitType = methodInfo.ReturnType.GetTypeFromTypeCode();
                        }

                        fieldName = methodInfo.Name;
                        fieldType = Mapping.GetMappingType(explicitType ?? methodInfo.ReturnType);
                        break;
                    }

                case MemberExpression memberExpression:
                    {
                        if(memberExpression.Member is FieldInfo fieldInfo
                            && memberExpression.Expression is ConstantExpression constantExpression)
                        {
                            fieldName = fieldInfo.GetValue(constantExpression.Value).ToString();
                            fieldType = Mapping.GetMappingType(explicitType ?? constantExpression.Type);
                            break;
                        }

                        MemberInfo memberInfo = memberExpression.Member;

                        if(ArrayHelper.IsArrayCandidate(memberExpression.Type))
                        {
                            explicitType = memberExpression.Type.GetTypeFromTypeCode();
                        }

                        fieldName = memberInfo.Name;
                        fieldType = Mapping.GetMappingType(explicitType ?? memberExpression.Type);
                        break;
                    }

                case ConstantExpression constantExpression:
                    if(ArrayHelper.IsArrayCandidate(constantExpression.Type))
                    {
                        explicitType = constantExpression.Type.GetTypeFromTypeCode();
                    }

                    fieldName = constantExpression.Value.ToString();
                    fieldType = Mapping.GetMappingType(explicitType ?? constantExpression.Type);
                    break;
                case UnaryExpression unaryExpression:
                    return GetFieldInfoFromExpression(unaryExpression.Operand, explicitType);
                default:
                    return null;
            }

            return new Tuple<string, MappingType>(fieldName, fieldType);
        }

        internal static string GetFieldName(Expression<Action<T>> fieldSelector)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfoFromExpression(fieldSelector.Body, null);

            return fieldInfo.Item1;
        }

        internal static string GetFieldName<TValue>(Expression<Func<T, TValue>> fieldSelector)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfoFromExpression(fieldSelector.Body, null);

            return fieldInfo.Item1;
        }
    }
}
#pragma warning restore 693
