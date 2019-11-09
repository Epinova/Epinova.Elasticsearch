using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Properties;

#pragma warning disable 693
namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IElasticSearchService<T> : IElasticSearchServiceFilters<T>
    {
        CultureInfo CurrentLanguage { get; }
        CultureInfo SearchLanguage { get; }
        int RootId { get; }
        Type Type { get; }
        Type SearchType { get; set; }
        string SearchText { get; }
        string IndexName { get; }
        Operator Operator { get; }
        bool UseBoosting { get; }
        int FromValue { get; }
        int SizeValue { get; }
        bool IsWildcard { get; }
        bool IsGetQuery { get; }
        bool TrackSearch { get; }

        /// <summary>
        /// Set your index name here if you want to use a different index from what is given in configuration.
        /// </summary>
        IElasticSearchService<T> UseIndex(string index);

        /// <summary>
        /// Boost the specified field when searching
        /// </summary>
        /// <param name="fieldSelector">Field expression</param>
        /// <param name="weight">Boost value</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Boost(Expression<Func<T, object>> fieldSelector, byte weight);

        /// <summary>
        /// Boost the specified type <typeparam name="TBoost"></typeparam>when searching
        /// </summary>
        /// <param name="weight">Boost value</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Boost<TBoost>(sbyte weight);

        /// <summary>
        /// Boost the specified type <paramref name="type"/> when searching
        /// </summary>
        /// <param name="type">The type to boost</param>
        /// <param name="weight">Boost value</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Boost(Type type, sbyte weight);

        /// <summary>
        /// Boost children based on ancestor path
        /// </summary>
        /// <param name="path">The ancestor id</param>
        /// <param name="weight">Boost value</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> BoostByAncestor(int path, sbyte weight);

        /// <summary>
        /// Decay hits based on date-property <paramref name="fieldSelector"/>. Default scale is 30 days.
        /// </summary>
        /// <param name="fieldSelector"><see cref="DateTime"/> field expression</param>
        /// <param name="scale">Substract 0.5 points for every time this value is reached. Defaults to 30 days if not supplied</param>
        /// <param name="offset">Don't apply decaying to hits with <paramref name="fieldSelector"/> lower than this value. Defaults to zero if not supplied</param>
        IElasticSearchService<T> Decay(Expression<Func<T, DateTime?>> fieldSelector, TimeSpan scale = default, TimeSpan offset = default);

        /// <summary>
        /// Decay hits based on date-property <paramref name="fieldName"/>. Default scale is 30 days.
        /// </summary>
        /// <param name="fieldName">The name of a <see cref="DateTime"/> field</param>
        /// <param name="scale">Substract 0.5 points for every time this value is reached. Defaults to 30 days if not supplied</param>
        /// <param name="offset">Don't apply decaying to hits with <paramref name="fieldName"/> lower than this value. Defaults to zero if not supplied</param>
        IElasticSearchService<T> Decay(string fieldName, TimeSpan scale = default, TimeSpan offset = default);

        /// <summary>
        /// Search with fuzziness
        /// </summary>
        /// <param name="length">The fuzzy length. Defaults to "AUTO" (recommended)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Fuzzy(byte? length = null);

        /// <summary>
        /// Search in the specified field. Can be called multiple times.
        /// </summary>
        /// <param name="fieldSelector">Field expression</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> InField(Expression<Func<T, object>> fieldSelector);

        /// <summary>
        /// Search in the specified field. Can be called multiple times for a selection of fields
        /// </summary>
        /// <param name="fieldSelector">Field expression</param>
        /// <param name="boost">Boost this field</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> InField(Expression<Func<T, object>> fieldSelector, bool boost);

        /// <summary>
        /// Exclude type <typeparamref name="TType"/> in current query
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Exclude<TType>();

        /// <summary>
        /// Exclude type <paramref name="type"/> in current query
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Exclude(Type type);

        /// <summary>
        /// Exclude the specified root id and its children from the query
        /// </summary>
        /// <param name="rootId">Id of the root to be excluded</param>
        /// <param name="recursive">Exclude <paramref name="rootId"/>'s children as well</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Exclude(int rootId, bool recursive = true);

        /// <summary>
        /// Create facets for field <paramref name="fieldSelector"/>
        /// </summary>
        /// <param name="fieldSelector">Field selector</param>
        /// <param name="usePostFilter">Apply filters after aggregations have been calculated.</param>
        /// <param name="explicitType">Force an explicit type conversion</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FacetsFor(Expression<Func<T, object>> fieldSelector, bool usePostFilter = true, Type explicitType = null);

        /// <summary>
        /// Sets the language to search in. Normally this will be infered from the current thread's culture
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Language(CultureInfo language);

        /// <summary>
        /// Searches within a range
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(string fieldName, int greaterThan, int lessThan);

        /// <summary>
        /// Searches for a <see cref="DateTime"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(string fieldName, DateTime greaterThan, DateTime? lessThan = null);

        /// <summary>
        /// Searches for a <see cref="Int64"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, or <see cref="Char"/></remarks>
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(string fieldName, long greaterThan, long? lessThan = null);

        /// <summary>
        /// Searches for a <see cref="Decimal"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(string fieldName, decimal greaterThan, decimal? lessThan = null);

        /// <summary>
        /// Searches for a <see cref="Double"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(string fieldName, double greaterThan, double? lessThan = null);

        /// <summary>
        /// Searches within a range
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(Expression<Func<T, IntegerRange>> fieldSelector, int greaterThan, int lessThan);

        /// <summary>
        /// Searches for a <see cref="DateTime"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// </summary>
        /// <param name="fieldSelector">DateTime field expression</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(Expression<Func<T, DateTime?>> fieldSelector, DateTime greaterThan, DateTime? lessThan = null);

        /// <summary>
        /// Searches for a <see cref="Int64"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, or <see cref="Char"/></remarks>
        /// </summary>
        /// <param name="fieldSelector">long field expression</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(Expression<Func<T, long?>> fieldSelector, long greaterThan, long? lessThan = null);

        /// <summary>
        /// Searches for a <see cref="Double"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldSelector"><see cref="Double"/> field expression</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(Expression<Func<T, double?>> fieldSelector, double greaterThan, double? lessThan = null);

        /// <summary>
        /// Searches for a <see cref="Decimal"/> value greater than <paramref name="greaterThan"/> 
        /// and optionally less than <paramref name="lessThan"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldSelector"><see cref="Decimal"/> field expression</param>
        /// <param name="greaterThan">The min-value</param>
        /// <param name="lessThan">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Range(Expression<Func<T, decimal?>> fieldSelector, decimal greaterThan, decimal? lessThan = null);

        /// <summary>
        /// Inclusive search within a range
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(string fieldName, int greaterThanOrEqualTo, int lessThanOrEqualTo);

        /// <summary>
        /// Searches for a <see cref="DateTime"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(string fieldName, DateTime greaterThanOrEqualTo, DateTime? lessThanOrEqualTo = null);

        /// <summary>
        /// Searches for a <see cref="Int64"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, or <see cref="Char"/></remarks>
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(string fieldName, long greaterThanOrEqualTo, long? lessThanOrEqualTo = null);

        /// <summary>
        /// Searches for a <see cref="Double"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(string fieldName, double greaterThanOrEqualTo, double? lessThanOrEqualTo = null);

        /// <summary>
        /// Searches for a <see cref="Decimal"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldName"> name of field</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(string fieldName, decimal greaterThanOrEqualTo, decimal? lessThanOrEqualTo = null);

        /// <summary>
        /// Inclusive search within a range
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(Expression<Func<T, IntegerRange>> fieldSelector, int greaterThanOrEqualTo, int lessThanOrEqualTo);

        /// <summary>
        /// Searches for a <see cref="DateTime"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// </summary>
        /// <param name="fieldSelector">DateTime field expression</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(Expression<Func<T, DateTime?>> fieldSelector, DateTime greaterThanOrEqualTo, DateTime? lessThanOrEqualTo = null);

        /// <summary>
        /// Searches for a <see cref="Int64"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, or <see cref="Char"/></remarks>
        /// </summary>
        /// <param name="fieldSelector">long field expression</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(Expression<Func<T, long?>> fieldSelector, long greaterThanOrEqualTo, long? lessThanOrEqualTo = null);

        /// <summary>
        /// Searches for a <see cref="Double"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldSelector"><see cref="Double"/> field expression</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(Expression<Func<T, double?>> fieldSelector, double greaterThanOrEqualTo, double? lessThanOrEqualTo = null);

        /// <summary>
        /// Searches for a <see cref="Decimal"/> value greater than or equal to <paramref name="greaterThanOrEqualTo"/> 
        /// and optionally less than or equal to <paramref name="lessThanOrEqualTo"/> (exclusive)
        /// <remarks>Supports explicit language conversions for <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Int16"/>, <see cref="UInt16"/>, <see cref="Int32"/>, <see cref="UInt32"/>, <see cref="UInt64"/>, <see cref="Char"/>, or <see cref="Single"/></remarks>
        /// </summary>
        /// <param name="fieldSelector"><see cref="Decimal"/> field expression</param>
        /// <param name="greaterThanOrEqualTo">The min-value</param>
        /// <param name="lessThanOrEqualTo">The max-value (optional)</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> RangeInclusive(Expression<Func<T, decimal?>> fieldSelector, decimal greaterThanOrEqualTo, decimal? lessThanOrEqualTo = null);

        /// <summary>
        /// Change what field to sort by.
        /// <remarks>
        /// Use with care, ElasticSearch usually gives you the best sorting based on score.
        /// </remarks>
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> SortBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector);

        /// <summary>
        /// Change what field to sort by.
        /// <remarks>
        /// Use with care, ElasticSearch usually gives you the best sorting based on score.
        /// </remarks>
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> SortBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min") where TProperty : GeoPoint;

        /// <summary>
        /// Change what field to sort by.
        /// <remarks>
        /// Use with care, ElasticSearch usually gives you the best sorting based on score.
        /// </remarks>
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> SortByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector);

        /// <summary>
        /// Change what field to sort by.
        /// <remarks>
        /// Use with care, ElasticSearch usually gives you the best sorting based on score.
        /// </remarks>
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> SortByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min") where TProperty : GeoPoint;

        /// <summary>
        /// <para>Advanced sorting using scripting</para>
        /// <para>See https://www.elastic.co/guide/en/elasticsearch/painless/current/index.html for scripting syntax</para>
        /// </summary>
        /// <param name="script">The script source</param>
        /// <param name="descending"><see langword="true"/> for descending or <see langword="false"/> ascending</param>
        /// <param name="type">The type of the script source, either <c>"string"</c> or <c>"number"</c></param>
        /// <param name="parameters">Anonymous object containing any parameters, eg. <c>new { Foo = 42 }</c></param>
        /// <param name="scriptLanguage">The script language, defaults to painless</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> SortByScript(string script, bool descending, string type = "string", object parameters = null, string scriptLanguage = null);

        /// <summary>
        /// Secondary field to sort by. Repeat as needed.
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min") where TProperty : GeoPoint;

        /// <summary>
        /// Secondary field to sort by. Repeat as needed.
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> ThenBy<TProperty>(Expression<Func<T, TProperty>> fieldSelector);

        /// <summary>
        /// Secondary field to sort by. Repeat as needed.
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector);

        /// <summary>
        /// Secondary field to sort by. Repeat as needed.
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> ThenByDescending<TProperty>(Expression<Func<T, TProperty>> fieldSelector, (double Lat, double Lon) compareTo, string unit = "km", string mode = "min") where TProperty : GeoPoint;

        /// <summary>
        /// Used to ignore any boosting set by <see cref="Epinova.ElasticSearch.Core.Attributes.BoostAttribute"/> 
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> NoBoosting();

        /// <summary>
        /// Lists all content of the specified type without using a search-term. Use in combination with SortBy() to list content 
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Get<T>();

        /// <summary>
        /// Skips the number of hits specified in <paramref name="from"/>
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> From(int from);

        /// <summary>
        /// Skips the number of hits specified in <paramref name="skip"/>
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Skip(int skip);

        /// <summary>
        /// How many hits to return (defaults to 10)
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Size(int size);

        /// <summary>
        /// How many hits to return (defaults to 10)
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Take(int take);

        /// <summary>
        /// Performs a standard search against all fields in all types
        /// </summary>
        /// <param name="searchText">The text to search for</param>
        /// <param name="operator">Specifies the operator to use, either <see cref="Enums.Operator.Or"/> or <see cref="Enums.Operator.And"/></param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<object> Search(string searchText, Operator @operator = Operator.Or);

        /// <summary>
        /// Performs a search against all fields in documents of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <param name="searchText">The text to search for</param>
        /// <param name="operator">Specifies the operator to use when searching, either <see cref="Enums.Operator.Or"/> or <see cref="Enums.Operator.And"/></param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Search<T>(string searchText, Operator @operator = Operator.Or);

        /// <summary>
        /// Performs a search against all fields in documents of the supplied type
        /// </summary>
        /// <typeparam name="T">The type to search for</typeparam>
        /// <param name="searchText">The text to search for</param>
        /// <param name="facetFieldName">Field to create facets from</param>
        /// <param name="operator">Specifies the operator to use when searching, either <see cref="Enums.Operator.Or"/> or <see cref="Enums.Operator.And"/></param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Search<T>(string searchText, string facetFieldName, Operator @operator = Operator.Or);

        /// <summary>
        /// Starting-point for search, ie. "Search from root in Episerver"
        /// </summary>
        /// <param name="id">The id of the root</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> StartFrom(int id);

        /// <summary>
        /// Materializes the search query and returns the results, 
        /// with the source fields in <paramref name="fields"/>
        /// </summary>
        /// <param name="enableHighlighting">Enable highlighting</param>
        /// <param name="enableDidYouMean">Include DidYouMean in query</param>
        /// <param name="applyDefaultFilters">Apply default filters, such as StopPublish</param>
        /// <param name="fields">Return these source fields</param>
        /// <returns>An instance of <see cref="SearchResult"/></returns>
        SearchResult GetResults(bool enableHighlighting = true, bool enableDidYouMean = true, bool applyDefaultFilters = true, params string[] fields);

        /// <summary>
        /// Materializes the search query and returns the results, 
        /// </summary>
        /// <returns>An instance of <see cref="CustomSearchResult{T}"/></returns>
        CustomSearchResult<T> GetCustomResults();

        /// <summary>
        /// Materializes the search query and returns the results, 
        /// </summary>
        /// <returns>An instance of <see cref="CustomSearchResult{T}"/></returns>
        Task<CustomSearchResult<T>> GetCustomResultsAsync();

        /// <summary>
        /// Materializes the search query and returns the results, 
        /// </summary>
        /// <returns>An instance of <see cref="CustomSearchResult{T}"/></returns>
        Task<CustomSearchResult<T>> GetCustomResultsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Materializes the search query and returns the results.
        /// All source fields are returned unless <paramref name="fields"/> is specified
        /// </summary>
        /// <param name="enableHighlighting">Enable highlighting</param>
        /// <param name="enableDidYouMean">Include DidYouMean in query</param>
        /// <param name="applyDefaultFilters">Apply default filters, such as StopPublish</param>
        /// <param name="fields">Return these source fields</param>
        /// <returns>An instance of <see cref="SearchResult"/></returns>
        Task<SearchResult> GetResultsAsync(bool enableHighlighting = true, bool enableDidYouMean = true, bool applyDefaultFilters = true, params string[] fields);

        /// <summary>
        /// Materializes the search query and returns the results
        /// as an async task.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token</param>
        /// <param name="enableHighlighting">Enable highlighting</param>
        /// <param name="enableDidYouMean">Include DidYouMean in query</param>
        /// <param name="applyDefaultFilters">Apply default filters, such as StopPublish</param>
        /// <param name="fields">Return these source fields</param>
        /// <returns>An instance of <see cref="SearchResult"/></returns>
        Task<SearchResult> GetResultsAsync(CancellationToken cancellationToken, bool enableHighlighting = true, bool enableDidYouMean = true, bool applyDefaultFilters = true, params string[] fields);

        /// <summary>
        /// Performs a generic wildcard query
        /// </summary>
        /// <param name="searchText">The text to search for</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<object> WildcardSearch(string searchText);

        /// <summary>
        /// Performs a generic wildcard query on type <typeparamref name="T"/>
        /// </summary>
        /// <param name="searchText">The text to search for</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> WildcardSearch<T>(string searchText);

        /// <summary>
        /// <para>
        /// Performs a more-like-this search, finding documents similar in content to the one identified by <paramref name="id"/>
        /// </para>
        /// <para>
        /// Use <see cref="InField(Expression{Func{T, Object}})"/> to control which fields to compare. 
        /// Defaults to «Name», «Description», «MainIntro», «MainBody»
        /// </para>
        /// </summary>
        /// <param name="id">Id of the document</param>
        /// <param name="minimumTermFrequency">The minimum term frequency below which the terms will be ignored from the input document. Defaults to 1.</param>
        /// <param name="maxQueryTerms">The maximum number of query terms that will be selected. Increasing this value gives greater accuracy at the expense of query execution speed. Defaults to 25.</param>
        /// <param name="minimumDocFrequency">The minimum document frequency below which the terms will be ignored from the input document. Defaults to 3.</param>
        /// <param name="minimumWordLength">The minimum word length below which the terms will be ignored. Defaults to 3.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> MoreLikeThis<T>(string id, int minimumTermFrequency = 1, int maxQueryTerms = 25, int minimumDocFrequency = 3, int minimumWordLength = 3);

        /// <summary>
        /// Perform multiple filters as a logical group
        /// </summary>
        /// <param name="groupExpression">The expression to perform ORs or ANDs on
        /// Defaults to <see cref="Epinova.ElasticSearch.Core.Enums.Operator.And"/></param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FilterGroup(Expression<Func<IFilterGroup<T>, IFilterGroup<T>>> groupExpression);

        /// <summary>
        /// Use a different query-time analyzer. Overrides analyzer in mapping.
        /// </summary>
        /// <param name="analyzer">Analyzer name</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> SetAnalyzer(string analyzer);

        /// <summary>
        /// Track search terms for this search
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Track();

        /// <summary>
        /// Boost terms defined as best bets
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> UseBestBets();

        /// <summary>
        /// Enables highlighting
        /// </summary>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Highlight();

        /// <summary>
        /// <para>Here be dragons</para>
        /// <para>Used to define a custom <c>script_score</c> affecting calculation of the score</para>
        /// </summary>
        /// <param name="script">The script source</param>
        /// <param name="scriptLanguage">The script language, defaults to painless</param>
        /// <param name="parameters">An anonymous object containing parameters to be used in <paramref name="script"/> </param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> CustomScriptScore(string script, string scriptLanguage = null, object parameters = null);
    }
}
#pragma warning restore 693
