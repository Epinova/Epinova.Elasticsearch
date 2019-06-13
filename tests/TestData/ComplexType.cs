using System;
using Epinova.ElasticSearch.Core.Models.Properties;
using EPiServer.DataAnnotations;

namespace TestData
{
    public class ComplexType
    {
        public ComplexType()
        {
            StringProperty = Factory.GetString();
            IntProperty = Factory.GetInteger();
            Id = Factory.GetInteger();
            DateTimeProperty = DateTime.Now;
        }

        public int Id { get; set; }

        [Searchable]
        public string StringProperty { get; set; }

        public int IntProperty { get; set; }

        public long LongProperty { get; set; }

        public double DoubleProperty { get; set; }

        public float FloatProperty { get; set; }

        public decimal DecimalProperty { get; set; }

        public DateTime DateTimeProperty { get; set; }

        public bool BoolProperty { get; set; }

        public GeoPoint GeoPointProperty { get; set; }
    }
}
