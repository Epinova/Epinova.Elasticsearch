using System;
using System.Globalization;
using Epinova.ElasticSearch.Core.Models.Bulk;
using TestData;
using Xunit;

namespace Core.Tests
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class BulkOperationTests
    {
        private ServiceLocatorFixture _fixture;

        public BulkOperationTests(ServiceLocatorFixture fixture)
        {
            _fixture = fixture;
            _fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.GetDefaultIndexName(new CultureInfo("de")))
                .Returns("my-index");

            _fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.GetDefaultIndexName(new CultureInfo("sv")))
                .Returns("delete-me");

        }

    
        [Fact]
        public void Ctor_TypeWithID_SetsMetaDataId()
        {
            var data = new ComplexType { Id = 42 };
            string indexName = _fixture.ServiceLocationMock.SettingsMock.Object.GetDefaultIndexName(new CultureInfo("en"));
            var result = new BulkOperation(indexName, data, Operation.Index);

            Assert.Equal(42, result.MetaData.Id);
        }

        [Fact]
        public void Ctor_SetsMetaDataDataType()
        {
            var data = new ComplexType { Id = 42 };
            string indexName = _fixture.ServiceLocationMock.SettingsMock.Object.GetDefaultIndexName(new CultureInfo("en"));
            var result = new BulkOperation(indexName, data, Operation.Index);

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

            string indexName = _fixture.ServiceLocationMock.SettingsMock.Object.GetDefaultIndexName(new CultureInfo("en"));
            var result = new BulkOperation(indexName, data, Operation.Index);
            dynamic resultData = result.Data;

            Assert.Equal(id.ToString(), resultData.Id);
            Assert.Equal(num.ToString(), resultData.IntProperty);
            Assert.Equal(flag, resultData.BoolProperty);
            Assert.Equal(text, resultData.StringProperty);
            Assert.Equal(date, resultData.DateTimeProperty);
        }
    }
}
