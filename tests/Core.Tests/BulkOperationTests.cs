using System;
using Epinova.ElasticSearch.Core.Models.Bulk;
using TestData;
using Xunit;

namespace Core.Tests
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class BulkOperationTests
    {
        [Fact]
        public void Ctor_EmptyLanguageAndIndex_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new BulkOperation(null, Operation.Index, null, null, null));
        }

        [Fact]
        public void Ctor_TypeWithID_SetsMetaDataId()
        {
            var data = new ComplexType { Id = 42 };
            var result = new BulkOperation(data, Operation.Index, "en", null, null, "test-en");

            Assert.Equal("42", result.MetaData.Id);
        }

        [Fact]
        public void Ctor_SetsMetaDataDataType()
        {
            var data = new ComplexType { Id = 42 };
            var result = new BulkOperation(data, Operation.Index, "en", null, null, "test-en");

            Assert.True(result.MetaData.DataType.IsAssignableFrom(typeof(ComplexType)));
        }

        [Fact]
        public void Ctor_SetsData()
        {
            const int id = 42;
            const int num = 123;
            const bool flag = true;
            const string text = "test";
            var date = new DateTime(1980, 1, 30);

            var data = new ComplexType
            {
                Id = id,
                IntProperty = num,
                BoolProperty = flag,
                StringProperty = text,
                DateTimeProperty = date
            };

            var result = new BulkOperation(data, Operation.Index, "en", null, null, "test-en");
            dynamic resultData = result.Data;

            Assert.Equal(id.ToString(), resultData.Id);
            Assert.Equal(num.ToString(), resultData.IntProperty);
            Assert.Equal(flag, resultData.BoolProperty);
            Assert.Equal(text, resultData.StringProperty);
            Assert.Equal(date, resultData.DateTimeProperty);
        }
    }
}
