using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Epinova.ElasticSearch.Core.Enums;
using EPiServer.Security;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IElasticSearchServiceFilters<T>
    {
        /// <summary>
        /// Filters the facets in the current query
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Filter<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue, bool raw = true);

        /// <summary>
        /// Filters the facets in the current query
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Filter<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue, bool raw = true);

        /// <summary>
        /// Filters the facets in the current query
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <param name="operator">Should we AND or OR the values?</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Filters<TType>(Expression<Func<T, TType>> fieldSelector, IEnumerable<TType> filterValues, Operator @operator = Operator.Or, bool raw = true);

        /// <summary>
        /// Filters the facets in the current query
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Filter<TType>(Expression<Action<T>> fieldSelector, TType filterValue, bool raw = true);

        /// <summary>
        /// Filters the facets in the current query
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <param name="operator">Should we AND or OR the values?</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Filters<TType>(Expression<Action<T>> fieldSelector, IEnumerable<TType> filterValues, Operator @operator = Operator.Or, bool raw = true);

        /// <summary>
        /// Filters the facets in the current query
        /// </summary>
        /// <param name="fieldName">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Filter<TType>(string fieldName, TType filterValue, bool raw = true);

        /// <summary>
        /// Filters the facets in the current query
        /// </summary>
        /// <param name="fieldName">The field to filter on</param>
        /// <param name="filterValues">The values to filter</param>
        /// <param name="operator">Should we AND or OR the values?</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> Filters<TType>(string fieldName, IEnumerable<TType> filterValues, Operator @operator = Operator.Or, bool raw = true);

        /// <summary>
        /// Filter away hits NOT matching <paramref name="filterValue"/>
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FilterMustNot<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue, bool raw = true);

        /// <summary>
        /// Filter away hits NOT matching <paramref name="filterValue"/>
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FilterMustNot<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue, bool raw = true);

        /// <summary>
        /// Filter away hits NOT matching <paramref name="filterValues"/>
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FiltersMustNot<TType>(Expression<Func<T, TType>> fieldSelector, IEnumerable<TType> filterValues, bool raw = true);

        /// <summary>
        /// Filter away hits NOT matching <paramref name="filterValue"/>
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FilterMustNot<TType>(Expression<Action<T>> fieldSelector, TType filterValue, bool raw = true);

        /// <summary>
        /// Filter away hits NOT matching <paramref name="filterValues"/>
        /// </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FiltersMustNot<TType>(Expression<Action<T>> fieldSelector, IEnumerable<TType> filterValues, bool raw = true);

        /// <summary>
        /// Filter away hits NOT matching <paramref name="filterValue"/>
        /// </summary>
        /// <param name="fieldName">The field to filter on</param>
        /// <param name="filterValue">The value to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FilterMustNot<TType>(string fieldName, TType filterValue, bool raw = true);

        /// <summary>
        /// Filter away hits NOT matching <paramref name="filterValues"/>
        /// </summary>
        /// <param name="fieldName">The field to filter on</param>
        /// <param name="filterValues">The values to filter</param>
        /// <param name="raw">Indicates that no analyzer nor tokenizer should be used.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FiltersMustNot<TType>(string fieldName, IEnumerable<TType> filterValues, bool raw = true);

        /// <summary>
        /// Require the current principal to have least read-access to hits.
        /// </summary>
        /// <param name="principal">The principal used to compare against ACL of hits. Defaults to <see cref="PrincipalInfo.Current"/> if null.</param>
        /// <returns>The current <see cref="IElasticSearchService"/> instance</returns>
        IElasticSearchService<T> FilterByACL(PrincipalInfo principal = null);
    }
}
