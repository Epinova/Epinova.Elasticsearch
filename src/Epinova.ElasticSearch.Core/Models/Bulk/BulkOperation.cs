using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Logging;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public class BulkOperation
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(BulkOperation));

        private static readonly NumberFormatInfo DotSeparatorFormat = new NumberFormatInfo
        {
            NumberGroupSeparator = String.Empty,
            NumberDecimalSeparator = "."
        };

        public BulkMetadata MetaData { get; }
        public object Data { get; }

        /// <summary>
        /// Creates a bulk-operation to be used in <see cref="CoreIndexer.Bulk(Epinova.ElasticSearch.Core.Models.Bulk.BulkOperation[])"/>. 
        /// </summary>
        public BulkOperation(string index, object data, string id = null) : this(index, data, Operation.Index, id: id)
        {
        }

        /// <summary>
        /// Creates a bulk-operation to be used in <see cref="CoreIndexer.Bulk(Epinova.ElasticSearch.Core.Models.Bulk.BulkOperation[])"/>. 
        /// Uses configured index if <paramref name="index"/> is empty.
        /// </summary>
        internal BulkOperation(string index, object data, Operation operation, Type dataType = null, string id = null)
        {
            dataType ??= data.GetType();

            id = GetId(id, dataType, data);

            // If we have no Types, this is a custom object and we must extract the properties from the data-object.
            // Standard IndexItems will already have needed data created by AsIndexItem
            if(dataType.GetProperty(DefaultFields.Types) == null)
            {
                dynamic indexItem = new ExpandoObject();
                var dictionary = (IDictionary<string, object>)indexItem;

                foreach(var property in data.GetType().GetProperties())
                {
                    try
                    {
                        var value = GetPropertyValue(data, property);
                        if(value != null)
                        {
                            dictionary[property.Name] = value;
                        }
                    }
                    catch(Exception ex)
                    {
                        Logger.Error("Failed to apply object property: " + property.Name, ex);
                    }
                }

                dictionary[DefaultFields.Types] = dataType.GetInheritancHierarchyArray();
                if(dataType.GetProperty(DefaultFields.Type) == null)
                {
                    dictionary.Add(DefaultFields.Type, dataType.GetTypeName());
                }

                data = indexItem;
            }

            MetaData = new BulkMetadata
            {
                Operation = operation,
                DataType = dataType,
                Type = dataType.GetTypeName(),
                Id = id,
                IndexCandidate = index?.ToLower()
            };

            Data = data;
        }

        private static object GetPropertyValue(object data, PropertyInfo property)
        {
            object value = property.GetValue(data);
            if(value == null)
                return null;

            if(property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                return (bool)value;

            if(property.PropertyType.IsEnum)
                return (int)value;
            
            if(value is DateTime)
            {
                // Don't ToString or anything funky here
                return value;
            }

            if(value is decimal dec)
                return dec.ToString(DotSeparatorFormat);

            if(value is double dbl)
                return dbl.ToString(DotSeparatorFormat);

            if(value is float flt)
                return flt.ToString(DotSeparatorFormat);

            if(ArrayHelper.IsArrayCandidate(property))
                return ArrayHelper.ToArray(value);

            if(Utilities.Mapping.IsNumericType(property.PropertyType))
                return value.ToString().Trim('\"');

            if(property.PropertyType.IsValueType || property.PropertyType.IsPrimitive)
                return value.ToString().Trim('\"');
            
            return value.ToString().Trim('\"');
        }

        private string GetId(string id, Type dataType, object data)
        {
            if(id != null)
            {
                return id;
            }

            var idProp = dataType.GetProperty(DefaultFields.Id);
            if(idProp != null)
            {
                var idVal = idProp.GetValue(data);
                if(idVal != null)
                {
                    return idVal.ToString();
                }
            }

            return null;
        }
    }
}
