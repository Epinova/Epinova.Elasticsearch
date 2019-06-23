using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Epinova.ElasticSearch.Core.Conventions;
using Epinova.ElasticSearch.Core.Settings;
using EPiServer.Logging;
using EPiServer.ServiceLocation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Epinova.ElasticSearch.Core.Utilities
{
    internal static class HttpClientHelper
    {
        private static readonly ILogger Logger = LogManager.GetLogger(typeof(HttpClientHelper));
        internal static readonly HttpClient Client = SetupClient();

        private static HttpClient SetupClient()
        {
            var client = MessageHandler.Handler != null
                ? new HttpClient(MessageHandler.Handler)
                : new HttpClient();

            IElasticSearchSettings settings = ServiceLocator.Current.GetInstance<IElasticSearchSettings>();

            if (!String.IsNullOrEmpty(settings.Username)
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

            if (settings.ClientTimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(settings.ClientTimeoutSeconds);
            }

            return client;
        }

        internal static void Put(Uri uri, byte[] data = null)
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
            catch (Exception ex)
            {
                Logger.Error("Request failed", ex);
            }
        }

        internal static async Task PutAsync(Uri uri, byte[] data = null)
        {
            await PutAsync(uri, data, CancellationToken.None).ConfigureAwait(false);
        }

        internal static async Task PutAsync(Uri uri, byte[] data, CancellationToken cancellationToken)
        {
            data = data ?? new byte[0];
            Logger.Debug($"Uri: {uri}, Data:\n{data}");

            try
            {
                HttpResponseMessage response = await Client.PutAsync(uri, JsonContent(data), cancellationToken).ConfigureAwait(false);
                LogErrorIfNotSuccess(response);
            }
            catch (Exception ex)
            {
                Logger.Error("Request failed", ex);
            }
        }

        internal static byte[] Post(Uri uri, byte[] data = null)
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
            catch (Exception ex)
            {
                Logger.Error("Request failed", ex);
                return null;
            }
        }

        internal static async Task<byte[]> PostAsync(Uri uri, byte[] data, CancellationToken cancellationToken)
        {
            data = data ?? new byte[0];
            Logger.Debug($"Uri: {uri}, Data:\n{data}");

            try
            {
                HttpResponseMessage response = await Client.PostAsync(uri, JsonContent(data), cancellationToken).ConfigureAwait(false);
                LogErrorIfNotSuccess(response);
                return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Request failed", ex);
                return null;
            }
        }

        internal static string GetJson(Uri uri)
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

        internal static string GetString(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            return AsyncUtil.RunSync(() =>
                Client.GetStringAsync(uri)
            );
        }

        internal static async Task<string> GetStringAsync(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            return await Client.GetStringAsync(uri).ConfigureAwait(false);
        }

        internal static HttpStatusCode Head(Uri uri)
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
            catch (Exception ex)
            {
                Logger.Error($"Error in HEAD-request: {uri}", ex);
                return HttpStatusCode.InternalServerError;
            }
        }

        internal static async Task<HttpStatusCode> HeadAsync(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            try
            {
                HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri)).ConfigureAwait(false);

                HttpStatusCode statusCode = response.StatusCode;

                Logger.Debug($"Status: {statusCode}");
                return statusCode;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error in HEAD-request: {uri}", ex);
                return HttpStatusCode.InternalServerError;
            }
        }

        internal static bool Delete(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");

            HttpResponseMessage response = AsyncUtil.RunSync(() =>
                Client.DeleteAsync(uri)
            );

            HttpStatusCode statusCode = response.StatusCode;
            Logger.Debug($"Status: {statusCode}");
            return statusCode == HttpStatusCode.OK;
        }

        internal static async Task<bool> DeleteAsync(Uri uri)
        {
            Logger.Debug($"Uri: {uri}");
            HttpResponseMessage response = await Client.DeleteAsync(uri).ConfigureAwait(false);
            HttpStatusCode statusCode = response.StatusCode;
            Logger.Debug($"Status: {statusCode}");
            return statusCode == HttpStatusCode.OK;
        }

        private static void LogErrorIfNotSuccess(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
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
                    Logger.Error("Could not read error-response");
                }
            }
        }

        private static ByteArrayContent JsonContent(byte[] data)
        {
            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }
    }
}
