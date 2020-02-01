using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Contracts;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Utilities
{
    [ServiceConfiguration(typeof(IHttpClientHelper))]
    internal class HttpClientHelper : IHttpClientHelper
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(HttpClientHelper));
        internal static readonly HttpClient Client = SetupClient();

        public void Put(Uri uri, byte[] data = null)
        {
            data = data ?? new byte[0];
            Logger.Debug($"Uri: {uri}, Data:\n{data}");

            try
            {
                HttpResponseMessage response = AsyncUtil.RunSync(() =>
                    Client.PutAsync(uri, JsonContent(data))
                );

                LogErrorIfNotSuccess(response);
            }
            catch(Exception ex)
            {
                Logger.Error("Request failed", ex);
            }
        }

        public async Task PutAsync(Uri uri, byte[] data = null) => await PutAsync(uri, data, CancellationToken.None).ConfigureAwait(false);

        public async Task PutAsync(Uri uri, byte[] data, CancellationToken cancellationToken)
        {
            data = data ?? new byte[0];
            Logger.Debug($"Uri: {uri}, Data:\n{data}");

            try
            {
                HttpResponseMessage response = await Client.PutAsync(uri, JsonContent(data), cancellationToken).ConfigureAwait(false);
                LogErrorIfNotSuccess(response);
            }
            catch(Exception ex)
            {
                Logger.Error("Request failed", ex);
            }
        }

        public byte[] Post(Uri uri, byte[] data = null)
        {
            data = data ?? new byte[0];
            Logger.Debug($"Uri: {uri}, Data:\n{data}");

            try
            {
                HttpResponseMessage response = AsyncUtil.RunSync(() =>
                      Client.PostAsync(uri, JsonContent(data))
                );

                LogErrorIfNotSuccess(response);

                return AsyncUtil.RunSync(() =>
                      response.Content.ReadAsByteArrayAsync()
                );
            }
            catch(Exception ex)
            {
                Logger.Error("Request failed", ex);
                return null;
            }
        }

        public async Task<byte[]> PostAsync(Uri uri, byte[] data, CancellationToken cancellationToken)
        {
            data = data ?? new byte[0];
            Logger.Debug($"Uri: {uri}, Data:\n{data}");

            try
            {
                HttpResponseMessage response = await Client.PostAsync(uri, JsonContent(data), cancellationToken).ConfigureAwait(false);
                LogErrorIfNotSuccess(response);
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch(Exception ex)
            {
                Logger.Error("Request failed", ex);
                return null;
            }
        }

        public string GetJson(Uri uri)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("Accept", "application/json");

            HttpResponseMessage response = AsyncUtil.RunSync(() =>
                Client.SendAsync(request)
            );

            response.EnsureSuccessStatusCode();

            return AsyncUtil.RunSync(() =>
                response.Content.ReadAsStringAsync()
            );
        }

        public string GetString(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            return AsyncUtil.RunSync(() =>
                Client.GetStringAsync(uri)
            );
        }

        public async Task<string> GetStringAsync(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            return await Client.GetStringAsync(uri).ConfigureAwait(false);
        }

        public HttpStatusCode Head(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            try
            {
                HttpResponseMessage response = AsyncUtil.RunSync(() =>
                    Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri))
                );

                HttpStatusCode statusCode = response.StatusCode;
                Logger.Debug($"Status: {statusCode}");
                return statusCode;
            }
            catch(Exception ex)
            {
                Logger.Error($"Error in HEAD-request: {uri}", ex);
                return HttpStatusCode.InternalServerError;
            }
        }

        public async Task<HttpStatusCode> HeadAsync(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            try
            {
                HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).ConfigureAwait(false);

                HttpStatusCode statusCode = response.StatusCode;

                Logger.Debug($"Status: {statusCode}");
                return statusCode;
            }
            catch(Exception ex)
            {
                Logger.Error($"Error in HEAD-request: {uri}", ex);
                return HttpStatusCode.InternalServerError;
            }
        }

        public bool Delete(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            HttpResponseMessage response = AsyncUtil.RunSync(() =>
                Client.DeleteAsync(uri)
            );

            HttpStatusCode statusCode = response.StatusCode;
            Logger.Debug($"Status: {statusCode}");
            return statusCode == HttpStatusCode.OK;
        }

        public async Task<bool> DeleteAsync(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");
            HttpResponseMessage response = await Client.DeleteAsync(uri).ConfigureAwait(false);
            HttpStatusCode statusCode = response.StatusCode;
            Logger.Debug($"Status: {statusCode}");
            return statusCode == HttpStatusCode.OK;
        }

        private static void LogErrorIfNotSuccess(HttpResponseMessage response)
        {
            if(!response.IsSuccessStatusCode)
            {
                Logger.Error($"Got status: {response.StatusCode}");

                string error = AsyncUtil.RunSync(() =>
                     response.Content.ReadAsStringAsync()
                );

                // Assume the response is json
                try
                {
                    Logger.Error(JToken.Parse(error).ToString(Formatting.Indented));
                }
                catch
                {
                    Logger.Error($"Could not parse error-response: {error}.\n Status: {(int)response.StatusCode} {response.StatusCode}");
                }
            }
        }

        private static ByteArrayContent JsonContent(byte[] data)
        {
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        private static HttpClient SetupClient()
        {
            var client = MessageHandler.Instance.Handler != null
                ? new HttpClient(MessageHandler.Instance.Handler)
                : new HttpClient();

            IElasticSearchSettings settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

            if(!String.IsNullOrEmpty(settings.Username)
                && !String.IsNullOrEmpty(settings.Password))
            {
                var credentials = Encoding.ASCII.GetBytes(
                    String.Concat(settings.Username, ":", settings.Password));

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
            }
            else
            {
                client.DefaultRequestHeaders.Authorization = null;
            }

            if(settings.ClientTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(settings.ClientTimeoutSeconds);
            }

            return client;
        }
    }
}
