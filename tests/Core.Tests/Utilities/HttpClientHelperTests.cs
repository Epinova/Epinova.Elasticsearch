using System;
using System.Net.Http.Headers;
using System.Text;
using Epinova.ElasticSearch.Core.Utilities;
using TestData;
using Xunit;

namespace Core.Tests.Utilities
{
    [Collection(nameof(ServiceLocatiorCollection))]
    public class HttpClientHelperTests : IClassFixture<ServiceLocatorFixture>
    {
        private const string Password = "bar";
        private const string Username = "foo";
        private const int Timeout = 42;

        public HttpClientHelperTests(ServiceLocatorFixture fixture)
        {
            fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.Username).Returns(Username);
            fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.Password).Returns(Password);
            fixture.ServiceLocationMock.SettingsMock
                .Setup(m => m.ClientTimeoutSeconds).Returns(Timeout);
        }

        [Fact]
        public void CredentialsIsSet_SetsAuthHeader()
        {
            AuthenticationHeaderValue result = HttpClientHelper.Client.DefaultRequestHeaders.Authorization;

            var expectedParam = Convert.ToBase64String(Encoding.UTF8.GetBytes(Username + ":" + Password));
            const string expectedScheme = "Basic";

            Assert.Equal(expectedScheme, result.Scheme);
            Assert.Equal(expectedParam, result.Parameter);
        }

        [Fact]
        public void TimeoutIsSet_SetsTimeout()
        {
            TimeSpan result = HttpClientHelper.Client.Timeout;

            Assert.Equal(TimeSpan.FromSeconds(Timeout), result);
        }
    }
}