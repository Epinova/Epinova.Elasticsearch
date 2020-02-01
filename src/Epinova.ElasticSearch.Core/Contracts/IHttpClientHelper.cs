using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Epinova.ElasticSearch.Core.Contracts
{
    public interface IHttpClientHelper
    {
        bool Delete(Uri uri);
        Task<bool> DeleteAsync(Uri uri);
        string GetJson(Uri uri);
        string GetString(Uri uri);
        Task<string> GetStringAsync(Uri uri);
        HttpStatusCode Head(Uri uri);
        Task<HttpStatusCode> HeadAsync(Uri uri);
        byte[] Post(Uri uri, byte[] data = null);
        byte[] Post(Uri uri, Stream data = null);
        Task<byte[]> PostAsync(Uri uri, byte[] data, CancellationToken cancellationToken);
        void Put(Uri uri, byte[] data = null);
        Task PutAsync(Uri uri, byte[] data = null);
        Task PutAsync(Uri uri, byte[] data, CancellationToken cancellationToken);
    }
}
