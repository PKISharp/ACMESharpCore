using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ACMECLI
{
    public static class HttpUtil
    {
        private static HttpClient _Client;

        public static HttpClient Client
        {
            get
            {
                if (_Client == null)
                {
                    lock(typeof(HttpUtil))
                    {
                        if (_Client == null)
                        {
                            _Client = new HttpClient();
                        }
                    }
                }
                return _Client;
            }
        }

        public static async Task<string> GetStringAsync(string url)
        {
            var resp = await Client.GetAsync(url);

            if (resp.StatusCode != HttpStatusCode.OK)
            {
                if (resp.StatusCode == HttpStatusCode.NotFound)
                    return null;
                throw new Exception("HTTP request error:  "
                        + $"({resp.StatusCode}) {await resp.Content.ReadAsStringAsync()}");
            }

            return await resp.Content.ReadAsStringAsync();
        }
    }
}