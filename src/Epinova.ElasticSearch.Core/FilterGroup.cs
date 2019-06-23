using System;
using System.Linq.Expressions;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models;

namespace Epinova.ElasticSearch.Core
{
    public class FilterGroup<T> : IFilterGroup<T>
    {
        private readonly FilterGroupQuery _filterGroup;

        public FilterGroup(ElasticSearchService<T> service, string name)
        {
            if(!service.PostFilterGroups.ContainsKey(name))
            {
                service.PostFilterGroups[name] = new FilterGroupQuery();
            }

            _filterGroup = service.PostFilterGroups[name];
        }

        public string Name { get; set; }

        public object Value { get; set; }

        public Operator Operator { get; set; }

        public IFilterGroup<T> Or<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues)
            => Or(fieldSelector, filterValues, true);

        public IFilterGroup<T> Or<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues, bool raw)
        {
            _filterGroup.Filters.Add(
                new Filter(
                    ElasticSearchService<T>.GetFieldName(fieldSelector),
                    filterValues,
                    typeof(TType),
                    raw,
                    Operator.Or));

            return this;
        }

        public IFilterGroup<T> Or<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues)
            => Or(fieldSelector, filterValues, true);

        public IFilterGroup<T> Or<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues, bool raw)
        {
            _filterGroup.Filters.Add(
                new Filter(
                    ElasticSearchService<T>.GetFieldName(fieldSelector),
                    filterValues,
                    typeof(TType),
                    raw,
                    Operator.Or));

            return this;
        }

        public IFilterGroup<T> And<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues)
            => And(fieldSelector, filterValues, true);

        public IFilterGroup<T> And<TType>(Expression<Action<T>> fieldSelector, TType[] filterValues, bool raw)
        {
            _filterGroup.Filters.Add(
                new Filter(
                    ElasticSearchService<T>.GetFieldName(fieldSelector),
                    filterValues,
                    typeof(TType),
                    raw,
                    Operator.And));

            return this;
        }

        public IFilterGroup<T> And<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue)
            => And(fieldSelector, filterValue, true);

        public IFilterGroup<T> And<TType>(Expression<Func<T, TType[]>> fieldSelector, TType filterValue, bool raw)
        {
            _filterGroup.Filters.Add(
                new Filter(
                    ElasticSearchService<T>.GetFieldName(fieldSelector),
                    filterValue,
                    typeof(TType),
                    raw,
                    Operator.And));

            return this;
        }

        public IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues)
            => And(fieldSelector, filterValues, true);

        public IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType[] filterValues, bool raw)
        {
            _filterGroup.Filters.Add(
                new Filter(
                    ElasticSearchService<T>.GetFieldName(fieldSelector),
                    filterValues,
                    typeof(TType),
                    raw,
                    Operator.And));

            return this;
        }

        public IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue)
            => And(fieldSelector, filterValue, true);

        public IFilterGroup<T> And<TType>(Expression<Func<T, TType>> fieldSelector, TType filterValue, bool raw)
        {
            _filterGroup.Filters.Add(
                new Filter(
                    ElasticSearchService<T>.GetFieldName(fieldSelector),
                    filterValue,
                    typeof(TType),
                    raw,
                    Operator.And));

            return this;
        }
    }
}
