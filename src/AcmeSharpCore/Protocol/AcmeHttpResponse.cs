using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using AcmeSharpCore.Http;

namespace AcmeSharpCore.Protocol
{
    public class AcmeHttpResponse
    {
            private string _ContentAsString;

            public AcmeHttpResponse(HttpResponseMessage resp)
            {
                StatusCode = resp.StatusCode;
                Headers = resp.Headers;
                Links = new LinkCollection(Headers.GetValues(AcmeHttpConstants.LinkHeader));

                var rs = resp.GetResponseStream();
                using (var ms = new MemoryStream())
                {
                    rs.CopyTo(ms);
                    RawContent = ms.ToArray();
                }
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

            public ProblemDetailResponse ProblemDetail
            { get; set; }    }
}