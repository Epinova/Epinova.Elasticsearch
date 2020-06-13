using System;
using System.Collections.Generic;
using System.Globalization;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models.Query;
using EPiServer.Security;

namespace Epinova.ElasticSearch.Core.Models
{
    internal sealed class QuerySetup
    {
        internal QuerySetup()
        {
            BoostAncestors = new Dictionary<int, sbyte>();
            BoostFields = new Dictionary<string, byte>();
            BoostTypes = new Dictionary<Type, sbyte>();
            ExcludedTypes = new List<Type>();
            ExcludedRoots = new Dictionary<int, bool>();
            FacetFieldNames = new Dictionary<string, MappingType>();
            Filters = new List<Filter>();
            FilterGroups = new Dictionary<string, FilterGroupQuery>();
            Language = CultureInfo.CurrentCulture;
            Ranges = new List<RangeBase>();
            SearchFields = new List<string>();
            SortFields = new List<Sort>();
            Gauss = new List<Gauss>();
            UsePostfilters = true;
        }

        public Type Type { get; set; }
        public CultureInfo Language { get; set; }
        public List<string> SearchFields { get; set; }
        public List<Type> ExcludedTypes { get; set; }
        public Dictionary<int, bool> ExcludedRoots { get; set; }
        public Dictionary<int, sbyte> BoostAncestors;
        public Dictionary<string, byte> BoostFields { get; set; }
        public Dictionary<Type, sbyte> BoostTypes { get; set; }
        public Dictionary<string, MappingType> FacetFieldNames { get; set; }
        public Operator Operator { get; set; }
        public List<Sort> SortFields { get; set; }
        public string SortDirection { get; set; }
        public bool SortFieldTypeIsString { get; set; }
        public string SearchText { get; set; }
        public string MoreLikeId { get; set; }
        public int MltMinTermFreq { get; set; }
        public int MltMinDocFreq { get; set; }
        public int MltMinWordLength { get; set; }
        public int MltMaxQueryTerms { get; set; }
        public bool UseBoosting { get; set; }
        public int From { get; set; }
        public int RootId { get; set; }
        public int Size { get; set; }
        public bool IsWildcard { get; set; }
        public bool IsGetQuery { get; set; }
        public List<Filter> Filters { get; set; }
        public Dictionary<string, FilterGroupQuery> FilterGroups { get; set; }
        public Type SearchType { get; set; }
        public List<RangeBase> Ranges { get; set; }
        public string[] SourceFields { get; set; }
        public string FuzzyLength { get; set; }
        public string IndexName { get; set; }
        public List<Gauss> Gauss { get; set; }
        public ScriptScore ScriptScore { get; set; }
        public bool UsePostfilters { get; set; }
        public bool EnableDidYouMean { get; set; } = true;
        public bool EnableHighlighting { get; set; } = true;
        public bool UseBestBets { get; set; }
        public bool UseHighlight { get; set; }
        public string Analyzer { get; internal set; }
        public bool ApplyDefaultFilters { get; set; }
        public bool AppendAclFilters { get; internal set; }
        public PrincipalInfo AclPrincipal { get; internal set; }
        public Version ServerVersion { get; internal set; }
    }
}
