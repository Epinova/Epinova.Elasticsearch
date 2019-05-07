using System;
using System.Collections.Generic;
using System.Dynamic;
using Epinova.ElasticSearch.Core.Extensions;
using EPiServer.Logging;
using Epinova.ElasticSearch.Core.Settings;
using Epinova.ElasticSearch.Core.Utilities;
using EPiServer.ServiceLocation;
using System.Globalization;

namespace Epinova.ElasticSearch.Core.Models.Bulk
{
    public class BulkOperation
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(BulkOperation));
        private static readonly IElasticSearchSettings ElasticSearchSettings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

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
            if (String.IsNullOrWhiteSpace(language) && String.IsNullOrWhiteSpace(index))
                throw new InvalidOperationException("Either 'language' or 'index' must be specified.");

            dataType = dataType ?? data.GetType();

            id = GetId(id, dataType, data);

            // If we have no Types, this is a custom object and we must extract the properties from the data-object.
            // Standard IndexItems will already have needed data created by AsIndexItem
            if (dataType.GetProperty(DefaultFields.Types) == null)
            {
                dynamic indexItem = new ExpandoObject();
                var dictionary = (IDictionary<string, object>)indexItem;

                foreach (var property in data.GetType().GetProperties())
                {
                    try
                    {
                        var value = property.GetValue(data);
                        if (value == null)
                            continue;

                        if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                        {
                            value = (bool)value;
                        }
                        else if (property.PropertyType.IsEnum)
                        {
                            value = (int)value;
                        }
                        else if (value is DateTime)
                        {
                            // Don't ToString or anything funky here
                        }
                        else if (value is decimal dec)
                        {
                            value = dec.ToString(DotSeparatorFormat);
                        }
                        else if (value is double dbl)
                        {
                            value = dbl.ToString(DotSeparatorFormat);
                        }
                        else if (value is float flt)
                        {
                            value = flt.ToString(DotSeparatorFormat);
                        }
                        else if (ArrayHelper.IsArrayCandidate(property))
                        {
                            value = ArrayHelper.ToArray(value);
                        }
                        else if (Utilities.Mapping.IsNumericType(property.PropertyType))
                        {
                            value = value.ToString().Trim('\"');
                        }
                        else if (property.PropertyType.IsValueType || property.PropertyType.IsPrimitive)
                        {
                            value = value.ToString().Trim('\"');
                        }
                        else
                        {
                            value = value.ToString().Trim('\"');
                        }

                        dictionary[property.Name] = value;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Failed to apply object property: " + property.Name, ex);
                    }
                }

                dictionary[DefaultFields.Types] = dataType.GetInheritancHierarchyArray();
                if (dataType.GetProperty(DefaultFields.Type) == null)
                {
                    dictionary.Add(DefaultFields.Type, dataType.GetTypeName());
                }
                data = indexItem;
            }

            if (String.IsNullOrWhiteSpace(index))
                index = ElasticSearchSettings.GetDefaultIndexName(language);

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

        private string GetId(string id, Type dataType, object data)
        {
            if (id != null)
                return id;

            var idProp = dataType.GetProperty(DefaultFields.Id);
            if (idProp != null)
            {
                var idVal = idProp.GetValue(data);
                if (idVal != null)
                    return idVal.ToString();
            }

            return null;
        }
    }
}
