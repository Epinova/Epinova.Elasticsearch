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
        [Fact(Skip = "Review static behaviour")]
        public void CredentialsSet_SetsAuthHeader()
        {
            const string password = "bar";
            const string username = "foo";

            Factory.SetupServiceLocator(null, username, password);

            HttpClientHelper.Initialize();
            AuthenticationHeaderValue result = HttpClientHelper.Client.DefaultRequestHeaders.Authorization;

            string expectedParam = Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            const string expectedScheme = "Basic";

            Assert.Equal(expectedScheme, result.Scheme);
            Assert.Equal(expectedParam, result.Parameter);
        }
    }
}