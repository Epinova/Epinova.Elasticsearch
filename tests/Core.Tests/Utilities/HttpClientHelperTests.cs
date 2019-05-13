using System;
using System.Net.Http.Headers;
using System.Text;
using Epinova.ElasticSearch.Core.Utilities;
using TestData;
using Xunit;

namespace Core.Tests.Utilities
{
    public class HttpClientHelperTests
    {
        private const string Password = "bar";
        private const string Username = "foo";
        private const int Timeout = 42;

        public HttpClientHelperTests()
        {
            Factory.SetupServiceLocator(null, Username, Password, Timeout);
            HttpClientHelper.Initialize();
        }

        [Fact]
        public void CredentialsIsSet_SetsAuthHeader()
        {
            AuthenticationHeaderValue result = HttpClientHelper.Client.DefaultRequestHeaders.Authorization;

            string expectedParam = Convert.ToBase64String(Encoding.UTF8.GetBytes(Username + ":" + Password));
            const string expectedScheme = "Basic";

            Assert.Equal(expectedScheme, result.Scheme);
            Assert.Equal(expectedParam, result.Parameter);
        }

        [Fact]
        public void TimeoutIsSet_SetsTimeout()
        {
            Factory.SetupServiceLocator(timeout: Timeout);

            HttpClientHelper.Initialize();
            TimeSpan result = HttpClientHelper.Client.Timeout;

            Assert.Equal(TimeSpan.FromSeconds(Timeout), result);
        }
    }
}