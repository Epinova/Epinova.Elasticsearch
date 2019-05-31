using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Enums;
using Epinova.ElasticSearch.Core.Models;
using Epinova.ElasticSearch.Core.Models.Properties;
using Epinova.ElasticSearch.Core.Models.Query;

namespace Epinova.ElasticSearch.Core
{
    public partial class ElasticSearchService<T>
    {
        public IElasticSearchService<T> FilterGeoBoundingBox(Expression<Func<T, GeoPoint>> fieldSelector, (double Lat, double Lon) topLeft, (double Lat, double Lon) bottomRight)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);
            var fieldName = fieldInfo.Item1;
            var box = new GeoBoundingBox(fieldName, new GeoPoint(topLeft.Lat, topLeft.Lon), new GeoPoint(bottomRight.Lat, bottomRight.Lon));
            PostFilters.Add(new Filter(fieldName, box, typeof(GeoPoint), true, Operator.And));

            return this;
        }

        public IElasticSearchService<T> FilterGeoDistance(Expression<Func<T, GeoPoint>> fieldSelector, string distance, (double Lat, double Lon) center)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);
            var fieldName = fieldInfo.Item1;
            var dist = new GeoDistance(fieldName, distance, new GeoPoint(center.Lat, center.Lon));
            PostFilters.Add(new Filter(fieldName, dist, typeof(GeoPoint), true, Operator.And));

            return this;
        }

        public IElasticSearchService<T> FilterGeoPolygon(Expression<Func<T, GeoPoint>> fieldSelector, IEnumerable<(double Lat, double Lon)> points)
        {
            Tuple<string, MappingType> fieldInfo = GetFieldInfo(fieldSelector);
            var fieldName = fieldInfo.Item1;
            var poly = new GeoPolygon(fieldName, points.Select(p => new GeoPoint(p.Lat, p.Lon)));
            PostFilters.Add(new Filter(fieldName, poly, typeof(GeoPoint), true, Operator.And));

            return this;
        }
    }
}