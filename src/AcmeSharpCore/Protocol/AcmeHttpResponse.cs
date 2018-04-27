using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AcmeSharpCore.Http;

namespace AcmeSharpCore.Protocol
{
    public class AcmeHttpResponse
    {
        private string _ContentAsString;

        /// <summary>
        /// Works in conjunction with the static method to provide an async alternative.
        /// </summary>
        private AcmeHttpResponse()
        { }

        public AcmeHttpResponse(HttpResponseMessage resp)
        {
            StatusCode = resp.StatusCode;
            Headers = resp.Headers;
            Links = new LinkCollection(Headers.GetValues(AcmeHttpConstants.LinkHeader));

            // Call on this synchronously
            RawContent = resp.Content.ReadAsByteArrayAsync().Result;
        }

        public HttpStatusCode StatusCode
        { get; set; }

        public HttpResponseHeaders Headers
        { get; set; }

        public LinkCollection Links
        { get; set; }

        public byte[] RawContent
        { get; set; }

        public string ContentAsString
        {
            get
            {
                if (_ContentAsString == null)
                {
                    if (RawContent == null || RawContent.Length == 0)
                        return null;
                    using (var ms = new MemoryStream(RawContent))
                    {
                        using (var sr = new StreamReader(ms))
                        {
                            _ContentAsString = sr.ReadToEnd();
                        }
                    }
                }
                return _ContentAsString;
            }
        }

        public bool IsError
        { get; set; }

        public Exception Error
        { get; set; }

        public Messages.ProblemDetailResponse ProblemDetail
        { get; set; }

        /// <summary>
        /// Alternative conversion method supports async alternative to the public constructor.
        /// </summary>
        public static async Task<AcmeHttpResponse> FromAsync(HttpResponseMessage resp)
        {
            var r = new AcmeHttpResponse();

            r.StatusCode = resp.StatusCode;
            r.Headers = resp.Headers;
            r.Links = new LinkCollection(r.Headers.GetValues(AcmeHttpConstants.LinkHeader));

            // Call on this synchronously
            r.RawContent = await resp.Content.ReadAsByteArrayAsync();

            return r;
        }
    }
}