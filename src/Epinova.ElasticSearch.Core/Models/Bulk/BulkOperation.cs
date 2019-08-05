using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using Epinova.ElasticSearch.Core.Extensions;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.Logging;
using EPiServer.ServiceLocation;

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
        public BulkOperation(object data, string language, string id = null, string index = null) : this(data, Operation.Index, language, id: id, index: index)
        {
        }

        /// <summary>
        /// Creates a bulk-operation to be used in <see cref="CoreIndexer.Bulk(Epinova.ElasticSearch.Core.Models.Bulk.BulkOperation[])"/>. 
        /// Uses configured index if <paramref name="index"/> is empty.
        /// </summary>
        internal BulkOperation(object data, Operation operation, string language = null, Type dataType = null, string id = null, string index = null)
        {
            if(String.IsNullOrWhiteSpace(language) && String.IsNullOrWhiteSpace(index))
            {
                throw new InvalidOperationException("Either 'language' or 'index' must be specified.");
            }

            dataType = dataType ?? data.GetType();

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

            if(String.IsNullOrWhiteSpace(index))
            {
                var settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();
                index = settings.GetDefaultIndexName(language);
            }

            MetaData = new BulkMetadata
            {
                Operation = operation,
                DataType = dataType,
                Type = dataType.GetTypeName(),
                Id = id,
                IndexCandidate = index.ToLower()
            };

            Data = data;
        }

        private static object GetPropertyValue(object data, PropertyInfo property)
        {
            var value = property.GetValue(data);
            if(value == null)
            {
                return null;
            }

            if(property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
            {
                return (bool)value;
            }
            else if(property.PropertyType.IsEnum)
            {
                return (int)value;
            }
            else if(value is DateTime)
            {
                // Don't ToString or anything funky here
                return value;
            }
            else if(value is decimal dec)
            {
                return dec.ToString(DotSeparatorFormat);
            }
            else if(value is double dbl)
            {
                return dbl.ToString(DotSeparatorFormat);
            }
            else if(value is float flt)
            {
                return flt.ToString(DotSeparatorFormat);
            }
            else if(ArrayHelper.IsArrayCandidate(property))
            {
                return ArrayHelper.ToArray(value);
            }
            else if(Utilities.Mapping.IsNumericType(property.PropertyType))
            {
                return value.ToString().Trim('\"');
            }
            else if(property.PropertyType.IsValueType || property.PropertyType.IsPrimitive)
            {
                return value.ToString().Trim('\"');
            }
            else
            {
                return value.ToString().Trim('\"');
            }
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
