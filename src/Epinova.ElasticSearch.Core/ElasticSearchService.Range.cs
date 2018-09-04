using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;

namespace Epinova.ElasticSearch.Core
{
    public partial class ElasticSearchService<T>
    {
        private readonly List<RangeBase> _ranges;

        public IElasticSearchService<T> Range(
            Expression<Func<T, IntegerRange>> fieldSelector, int greaterThan, int lessThan)
        {
            return CreateIntegerRange(fieldSelector, greaterThan, lessThan);
        }


        public IElasticSearchService<T> Range(
            Expression<Func<T, long?>> fieldSelector,
            long greaterThan,
            long? lessThan = null)
        {
            return CreateRange(fieldSelector, greaterThan, lessThan);
        }


        public IElasticSearchService<T> Range(
            Expression<Func<T, double?>> fieldSelector,
            double greaterThan,
            double? lessThan = null)
        {
            return CreateRange(fieldSelector, greaterThan, lessThan);
        }


        public IElasticSearchService<T> Range(
            Expression<Func<T, decimal?>> fieldSelector,
            decimal greaterThan,
            decimal? lessThan = null)
        {
            return CreateRange(fieldSelector, greaterThan, lessThan);
        }


        public IElasticSearchService<T> Range(
            Expression<Func<T, DateTime?>> fieldSelector,
            DateTime greaterThan,
            DateTime? lessThan = null)
        {
            return CreateRange(fieldSelector, greaterThan, lessThan);
        }


        public IElasticSearchService<T> RangeInclusive(
            Expression<Func<T, DateTime?>> fieldSelector,
            DateTime greaterThanOrEqualTo,
            DateTime? lessThanOrEqualTo = null)
        {
            return CreateRange(fieldSelector, greaterThanOrEqualTo, lessThanOrEqualTo, true);
        }


        public IElasticSearchService<T> RangeInclusive(
            Expression<Func<T, double?>> fieldSelector,
            double greaterThanOrEqualTo,
            double? lessThanOrEqualTo = null)
        {
            return CreateRange(fieldSelector, greaterThanOrEqualTo, lessThanOrEqualTo, true);
        }


        public IElasticSearchService<T> RangeInclusive(
            Expression<Func<T, long?>> fieldSelector,
            long greaterThanOrEqualTo,
            long? lessThanOrEqualTo = null)
        {
            return CreateRange(fieldSelector, greaterThanOrEqualTo, lessThanOrEqualTo, true);
        }


        public IElasticSearchService<T> RangeInclusive(
            Expression<Func<T, decimal?>> fieldSelector,
            decimal greaterThanOrEqualTo,
            decimal? lessThanOrEqualTo = null)
        {
            return CreateRange(fieldSelector, greaterThanOrEqualTo, lessThanOrEqualTo, true);
        }


        public IElasticSearchService<T> RangeInclusive(
            Expression<Func<T, IntegerRange>> fieldSelector, int greaterThanOrEqualTo, int lessThanOrEqualTo)
        {
            return CreateIntegerRange(fieldSelector, greaterThanOrEqualTo, lessThanOrEqualTo, true);
        }


        private IElasticSearchService<T> CreateIntegerRange(
            Expression<Func<T, IntegerRange>> fieldSelector,
            int greaterThan,
            int lessThan,
            bool inclusive = false,
            string relation = "intersects")
        {
            string fieldName = GetFieldName(fieldSelector);

            Range<int> range = new Range<int>(fieldName, true)
            {
                RangeSetting =
                {
                    Relation = relation,
                    Inclusive = inclusive
                }
            };


            if (inclusive)
            {
                range.RangeSetting.Gte = greaterThan;
                range.RangeSetting.Lte = lessThan;
            }
            else
            {
                range.RangeSetting.Gt = greaterThan;
                range.RangeSetting.Lt = lessThan;
            }

            _ranges.Add(range);

            return this;
        }


        private IElasticSearchService<T> CreateRange<TValue>(
            Expression<Func<T, TValue?>> fieldSelector,
            TValue greaterThan,
            TValue? lessThan,
            bool inclusive = false,
            string relation = null)
            where TValue : struct
        {
            string fieldName = GetFieldName(fieldSelector);
            Range<TValue> range = new Range<TValue>(fieldName, inclusive)
            {
                RangeSetting =
                {
                    Relation = relation
                }
            };

            if (inclusive)
            {
                range.RangeSetting.Gte = greaterThan;
                range.RangeSetting.Lte = lessThan;
            }
            else
            {
                range.RangeSetting.Gt = greaterThan;
                range.RangeSetting.Lt = lessThan;
            }

            _ranges.Add(range);

            return this;
        }
    }
}