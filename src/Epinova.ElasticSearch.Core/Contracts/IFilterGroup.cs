using System;
using System.Linq.Expressions;
using Epinova.ElasticSearch.Core.Enums;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IFilterGroup<T>
    {
        string Name { get; set; }

        object Value { get; set; }

        Operator Operator { get; set; }

        /// <summary> Adds an OR-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> Or<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues);


        /// <summary> Adds an OR-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <param name="raw">Use raw (keyword) field? Defaults to true</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> Or<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues, bool raw);


        /// <summary> Adds an OR-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> Or<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues);


        /// <summary> Adds an OR-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <param name="raw">Use raw (keyword) field? Defaults to true</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> Or<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues, bool raw);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <param name="raw">Use raw (keyword) field? Defaults to true</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues, bool raw);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter by</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter by</param>
        /// <param name="raw">Use raw (keyword) field? Defaults to true</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue, bool raw);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValues">The values to filter by</param>
        /// <param name="raw">Use raw (keyword) field? Defaults to true</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues, bool raw);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter by</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue);


        /// <summary> Adds an AND-expression to the filter group </summary>
        /// <param name="fieldSelector">The field to filter on</param>
        /// <param name="filterValue">The value to filter by</param>
        /// <param name="raw">Use raw (keyword) field? Defaults to true</param>
        /// <returns>The filter group</returns>
        IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue, bool raw);
    }
}