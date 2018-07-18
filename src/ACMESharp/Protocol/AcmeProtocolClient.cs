using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Logging;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Messages;
using ACMESharp.Protocol.Resources;
using _Authorization = ACMESharp.Protocol.Resources.Authorization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ACMESharp.Protocol
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7
    /// </summary>
    public class AcmeProtocolClient : IDisposable
    {
        private static readonly HttpStatusCode[] SkipExpectedStatuses = new HttpStatusCode[0];

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private bool _disposeHttpClient;
        private HttpClient _http;
        private IJwsTool _signer;
        private ILogger _log;

        public AcmeProtocolClient(HttpClient http, ServiceDirectory dir = null,
                AccountDetails acct = null, IJwsTool signer = null,
                bool disposeHttpClient = false,
                ILogger logger = null)
        {
            Init(http, dir, acct, signer, logger);
            _disposeHttpClient = disposeHttpClient;
        }

        public AcmeProtocolClient(Uri baseUri, ServiceDirectory dir = null,
                AccountDetails acct = null, IJwsTool signer = null,
                ILogger logger = null)
        {
            var http = new HttpClient
            {
                BaseAddress = baseUri,
            };
            Init(http, dir, acct, signer, logger);
            _disposeHttpClient = true;
        }

        private void Init(HttpClient http, ServiceDirectory dir,
                AccountDetails acct, IJwsTool signer,
                ILogger logger)
        {
            _http = http;
            Directory = dir ?? new ServiceDirectory();

            Account = acct;

            Signer = signer ?? ResolveDefaultSigner();

            _log = logger ?? NullLogger.Instance;
            _log.LogInformation("ACME client initialized");
        }

        private IJwsTool ResolveDefaultSigner()
        {
            // We default to ES256 signer
            var signer = new Crypto.JOSE.Impl.ESJwsTool();
            signer.Init();
            return signer;
        }

        /// <summary>
        /// A tool that can be used to JWS-sign request messages to the
        /// target ACME server.
        /// </summary>
        /// <remarks>
        /// If not specified during construction, a default signing tool
        /// with a new set of keys will be constructed of type ES256
        /// (Elliptic Curve using the P-256 curve and a SHA256 hash).
        /// </remarks>
        public IJwsTool Signer { get; private set; }

        public ServiceDirectory Directory { get; set; }

        public AccountDetails Account { get; set; }

        public string NextNonce { get; private set; }

        public Action<string, object> BeforeAcmeSign { get; set; }

        public Action<string, HttpRequestMessage> BeforeHttpSend { get; set; }

        public Action<string, HttpResponseMessage> AfterHttpSend { get; set; }

        /// <summary>
        /// Retrieves the Directory object from the target ACME CA.  The Directory is used
        /// to help clients configure themselves with the right URLs for each ACME operation.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.1
        /// </remarks>
        public async Task<ServiceDirectory> GetDirectoryAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            return await SendAcmeAsync<ServiceDirectory>(
                    new Uri(_http.BaseAddress, Directory.Directory),
                    skipNonce: true,
                    cancel: cancel);
        }

        /// <summary>
        /// Convenience routine to retrieve the raw bytes of the Terms of Service
        /// endpoint defined in an ACME Resource Directory meta details.
        /// </summary>
        /// <returns>Returns a tuple containing the content type, the filename as best
        ///         can be determined by the response headers or the request URL, and
        ///         the raw content bytes; typically this might resolve to a PDF file</returns>
        public async Task<(MediaTypeHeaderValue contentType,
                string filename, byte[] content)> GetTermsOfServiceAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var tosUrl = Directory?.Meta?.TermsOfService;
            if (tosUrl == null)
                return (null, null, null);
            
            using (var resp = await _http.GetAsync(tosUrl, cancel))
            {
                var filename = resp.Content?.Headers?.ContentDisposition?.FileName;
                if (string.IsNullOrEmpty(filename))
                    filename = new Uri(tosUrl).AbsolutePath;
                return (resp.Content.Headers.ContentType,
                        Path.GetFileName(filename),
                        await resp.Content.ReadAsByteArrayAsync());
            }
        }            

        /// <summary>
        /// Retrieves a fresh nonce to be used in subsequent communication
        /// between the client and target ACME CA.  The client might
        /// sometimes need to get a new nonce, e.g., on its first request
        /// to the server or if an existing nonce is no longer valid.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.2
        /// </remarks>
        public async Task GetNonceAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            // Some weird behavior here:
            // According to RFC, this should respond to HEAD request with 200
            // and to GET request with a 204, but we're seeing 204 for both

            await SendAcmeAsync(
                    new Uri(Directory.NewNonce),
                    method: HttpMethod.Head,
                    expectedStatuses: new[] { 
                        HttpStatusCode.OK,
                        HttpStatusCode.NoContent,
                    },
                    cancel: cancel);
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
        /// </remarks>
        public async Task<AccountDetails> CreateAccountAsync(IEnumerable<string> contacts,
            bool termsOfServiceAgreed = false,
            object externalAccountBinding = null,
            bool throwOnExistingAccount = false,
            CancellationToken cancel = default(CancellationToken))
        {
            var message = new CreateAccountRequest
            {
                Contact = contacts,
                TermsOfServiceAgreed = termsOfServiceAgreed,
                ExternalAccountBinding = (JwsSignedPayload)externalAccountBinding,
            };
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, Directory.NewAccount),
                    method: HttpMethod.Post,
                    message: message,
                    expectedStatuses: new[] { HttpStatusCode.Created, HttpStatusCode.OK },
                    includePublicKey: true,
                    cancel: cancel);

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                if (throwOnExistingAccount)
                    throw new InvalidOperationException("Existing account public key found");
            }

            var acct = await DecodeAccountResponseAsync(resp);
            
            if (string.IsNullOrEmpty(acct.Kid))
                throw new InvalidDataException(
                        "Account creation response does not include Location header");

            return acct;
        }

        /// <summary>
        /// Verifies that an Account exists in the target ACME CA that is associated
        /// associated with the current Account Public Key.  If the check succeeds,
        /// the returned Account  object will <b>only</b> have its <c>Kid</c>
        /// property populated -- all other fields will be empty.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.1
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.3
        /// <para>
        /// If the Account does not exist, then an exception is thrown.
        /// </para>
        /// </remarks>
        public async Task<AccountDetails> CheckAccountAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, Directory.NewAccount),
                    method: HttpMethod.Post,
                    message: new CheckAccountRequest(),
                    expectedStatuses: SkipExpectedStatuses,
                    includePublicKey: true,
                    cancel: cancel);

            if (resp.StatusCode == HttpStatusCode.BadRequest)
                throw new InvalidOperationException(
                        $"Invalid or missing account ({resp.StatusCode})");

            if (resp.StatusCode != HttpStatusCode.OK)
                throw await DecodeResponseErrorAsync(resp);

            var acct = await DecodeAccountResponseAsync(resp, existing: Account);

            if (string.IsNullOrEmpty(acct.Kid))
                throw new InvalidDataException(
                        "Account lookup response does not include Location header");

            return acct;
        }

        /// <summary>
        /// Updates existing Account information registered with the ACME CA.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.2
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.3
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.4
        /// </remarks>
        public async Task<AccountDetails> UpdateAccountAsync(IEnumerable<string> contacts,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, Account.Kid);
            var message = new UpdateAccountRequest
            {
                Contact = contacts,
            };
            var resp = await SendAcmeAsync(
                    requUrl,
                    method: HttpMethod.Post,
                    message: message,
                    cancel: cancel);

            var acct = await DecodeAccountResponseAsync(resp, existing: Account);

            if (string.IsNullOrEmpty(acct.Kid))
                throw new InvalidDataException(
                        "Account update response does not include Location header");

            return acct;
        }

        // TODO: handle "Change of TOS" error response
        //    https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.4


        /// <summary>
        /// Rotates the current Public key that is associated with this Account by the
        /// target ACME CA with a new Public key.  If successful, updates the current
        /// Account key pair registered with the client.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.6
        /// </remarks>
        public async Task<AccountDetails> ChangeAccountKeyAsync(IJwsTool newSigner,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, Directory.KeyChange);
            var message = new KeyChangeRequest
            {
                Account = Account.Kid,
                NewKey = newSigner.ExportJwk(),
            };
            var innerPayload = ComputeAcmeSigned(message, requUrl.ToString(),
                    signer: newSigner, includePublicKey: true, excludeNonce: true);
            var resp = await SendAcmeAsync(
                    requUrl,
                    method: HttpMethod.Post,
                    message: innerPayload,
                    cancel: cancel);

            Signer = newSigner;

            return await DecodeAccountResponseAsync(resp, existing: Account);
        }

        /// <summary>
        /// Deactivates the current Account associated with this client.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.7
        /// </remarks>
        public async Task<AccountDetails> DeactivateAccountAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var resp = await SendAcmeAsync(
                    new Uri(Account.Kid),
                    method: HttpMethod.Post,
                    message: new DeactivateAccountRequest(),
                    cancel: cancel);

            return await DecodeAccountResponseAsync(resp, existing: Account);
        }

        /// <summary>
        /// Creates a new Order for a Certificate which will contain one or more
        /// DNS Identifiers.  The first Identifier will be treated as the primary
        /// subject of the certificate, and any optional subsequent Identifiers
        /// will be treated as Subject Alterative Name (SAN) entries.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.3
        /// </remarks>
        public async Task<OrderDetails> CreateOrderAsync(IEnumerable<string> dnsIdentifiers,
            DateTime? notBefore = null,
            DateTime? notAfter = null,
            CancellationToken cancel = default(CancellationToken))
        {
            var message =  new CreateOrderRequest
            {
                Identifiers = dnsIdentifiers.Select(x =>
                        new Identifier { Type = "dns", Value = x}).ToArray(),

                // TODO: deal with dates
                // NotBefore = notBefore?.ToString(),
                // NotAfter = notAfter?.ToString(),
            };
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, Directory.NewOrder),
                    method: HttpMethod.Post,
                    message: message,
                    expectedStatuses: new[] { HttpStatusCode.Created },
                    cancel: cancel);

            var order = await DecodeOrderResponseAsync(resp);
            return order;
        }

        /// <summary>
        /// Retrieves the current status and details of an existing Order.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.3
        /// <para>
        /// You can optionally pass in an existing Order details object if this
        /// is refreshing the state of an existing one, and some values that
        /// don't change, but also are not supplied in subsequent requests, such
        /// as the Order URL, will be copied over.
        /// </para>
        /// </remarks>
        public async Task<OrderDetails> GetOrderDetailsAsync(string orderUrl,
            OrderDetails existing = null,
            CancellationToken cancel = default(CancellationToken))
        {
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, orderUrl),
                    skipNonce: true,
                    cancel: cancel);

            var order = await DecodeOrderResponseAsync(resp, existing);
            return order;

            // var resp = await SendAcmeAsync(
            //         new Uri(_http.BaseAddress, order.OrderUrl),
            //         skipNonce: true,
            //         cancel: cancel);

            // var coResp = JsonConvert.DeserializeObject<Order>(
            //         await resp.Content.ReadAsStringAsync());
            
            // var updatedOrder = new AcmeOrder
            // {
            //     OrderUrl = resp.Headers.Location?.ToString() ?? order.OrderUrl,
            //     Status = coResp.Status,
            //     Expires = coResp.Expires == null
            //         ? DateTime.MinValue
            //         : DateTime.Parse(coResp.Expires),
            //     DnsIdentifiers = coResp.Identifiers?.Select(x => x.Value).ToArray()
            //         ?? order.DnsIdentifiers,
            //     Authorizations = coResp.Authorizations?.Select(x =>
            //             new AcmeAuthorization { DetailsUrl = x }).ToArray()
            //         ?? order.Authorizations,
            //     FinalizeUrl = coResp.Finalize,
            //     CertificateUrl = coResp.Certificate,
            // };

            // foreach (var authz in updatedOrder.Authorizations)
            // {
            //     resp = await _http.GetAsync(authz.DetailsUrl);
            //     var body = await resp.Content.ReadAsStringAsync();

            //     if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(body))
            //     {
            //         authz.FetchError = $"Failed to retrieve details: {resp.StatusCode}";
            //         if (resp.Content != null)
            //         {
            //             authz.FetchError += $"; {body}";
            //         }
            //     }
            //     else
            //     {
            //         authz.Details = JsonConvert
            //             .DeserializeObject<Protocol.Model.Authorization>(body);
            //     }
            // }

            // return updatedOrder;
        }

        /// <summary>
        /// Retrieves the details of an Authorization associated with a previously
        /// created Order.  The Authorization details URL is returned as part of
        /// an Order's response.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.4
        /// <para>
        /// Use this operation to retrieve the initial details of an Authorization,
        /// such as immediately after creating a new Order, as well as to retrieve
        /// the subsequent state and progress of an Authorization, such as as after
        /// responding to an associated Challenge.
        /// </para>
        /// </remarks>
        public async Task<_Authorization> GetAuthorizationDetailsAsync(string authzDetailsUrl,
            CancellationToken cancel = default(CancellationToken))
        {
            var typedResp = await SendAcmeAsync<_Authorization>(
                    new Uri(_http.BaseAddress, authzDetailsUrl),
                    skipNonce: true,
                    cancel: cancel);
            
            return typedResp;
        }

        /// <summary>
        /// Deactivates a specific Authorization and thereby relinquishes the
        /// authority to issue Certificates for the associated Identifier.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.2
        /// </remarks>
        public async Task<_Authorization> DeactivateAuthorizationAsync(string authzDetailsUrl,
            CancellationToken cancel = default(CancellationToken))
        {
            var typedResp = await SendAcmeAsync<_Authorization>(
                    new Uri(_http.BaseAddress, authzDetailsUrl),
                    method: HttpMethod.Post,
                    message: new DeactivateAuthorizationRequest(),
                    cancel: cancel);
            
            return typedResp;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.1
        /// </remarks>
        public async Task<Challenge> GetChallengeDetailsAsync(string challengeDetailsUrl,
            CancellationToken cancel = default(CancellationToken))
        {
            var typedResp = await SendAcmeAsync<Challenge>(
                    new Uri(_http.BaseAddress, challengeDetailsUrl),
                    skipNonce: true,
                    cancel: cancel);

            return typedResp;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.1
        /// </remarks>
        public async Task<Challenge> AnswerChallengeAsync(string challengeDetailsUrl,
            CancellationToken cancel = default(CancellationToken))
        {
            var typedResp = await SendAcmeAsync<Challenge>(
                    new Uri(_http.BaseAddress, challengeDetailsUrl),
                    method: HttpMethod.Post,
                    // TODO:  for now, none of the challenge types
                    // take any input data to answer the challenge
                    message: new { },
                    cancel: cancel);
        
            return typedResp;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4
        /// </remarks>
        public async Task<OrderDetails> FinalizeOrderAsync(string orderFinalizeUrl,
            byte[] derEncodedCsr,
            CancellationToken cancel = default(CancellationToken))
        {
            var message = new FinalizeOrderRequest
            {
                Csr = CryptoHelper.Base64.UrlEncode(derEncodedCsr),
            };
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, orderFinalizeUrl),
                    method: HttpMethod.Post,
                    message: message,
                    cancel: cancel);

            return await DecodeOrderResponseAsync(resp);

            // var message = new FinalizeOrderRequest
            // {
            //     Csr = CryptoHelper.Base64UrlEncode(derEncodedCsr),
            // };
            // var resp = await SendAcmeAsync(
            //         new Uri(_http.BaseAddress, order.FinalizeUrl),
            //         method: HttpMethod.Post,
            //         message: message,
            //         cancel: cancel);

            // var coResp = JsonConvert.DeserializeObject<Order>(
            //         await resp.Content.ReadAsStringAsync());
            
            // var newOrder = new AcmeOrder
            // {
            //     OrderUrl = resp.Headers.Location?.ToString() ?? order.OrderUrl,
            //     Status = coResp.Status,
            //     Expires = coResp.Expires == null
            //         ? DateTime.MinValue
            //         : DateTime.Parse(coResp.Expires),
            //     DnsIdentifiers = coResp.Identifiers?.Select(x => x.Value).ToArray()
            //         ?? order.DnsIdentifiers,
            //     Authorizations = coResp.Authorizations?.Select(x =>
            //             new AcmeAuthorization { DetailsUrl = x }).ToArray()
            //         ?? order.Authorizations,
            //     FinalizeUrl = coResp.Finalize,
            // };

            // foreach (var authz in newOrder.Authorizations)
            // {
            //     resp = await _http.GetAsync(authz.DetailsUrl);
            //     var body = await resp.Content.ReadAsStringAsync();

            //     if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(body))
            //     {
            //         authz.FetchError = $"Failed to retrieve details: {resp.StatusCode}";
            //         if (resp.Content != null)
            //         {
            //             authz.FetchError += $"; {body}";
            //         }
            //     }
            //     else
            //     {
            //         authz.Details = JsonConvert
            //             .DeserializeObject<Protocol.Model.Authorization>(body);
            //     }
            // }

            // return newOrder;
        }

        /// <summary>
        /// Convenience routine to retrieve the raw Certificate bytes (PEM encoded)
        /// associated with a finalized ACME Order.
        /// </summary>
        public async Task<byte[]> GetOrderCertificateAsync(
            OrderDetails order,
            CancellationToken cancel = default(CancellationToken))
        {
            using (var resp = await GetAsync(order.Payload.Certificate, cancel))
            {
                return await resp.Content.ReadAsByteArrayAsync();
            }
        }

        /// <summary>
        /// Generic fetch routine to retrieve raw bytes from a URL associated
        /// with an ACME endpoint.
        /// </summary>
        /// <param name="relativeUrl">The URL to fetch which may be relative to the ACME
        ///         endpoint associated with this client instance</param>
        /// <param name="cancel">Optional cancellation token</param>
        /// <returns>A tuple containing the content type and the raw content bytes</returns>
        public async Task<HttpResponseMessage> GetAsync(
            string relativeUrl,
            CancellationToken cancel = default(CancellationToken))
        {
            var url = new Uri(_http.BaseAddress, relativeUrl);
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();
            return resp;
        }

        /// <summary>
        /// The workhorse routine for submitting HTTP requests using ACME protocol
        /// semantics and activating pre- and post-submission event hooks.
        /// </summary>
        /// <param name="uri">URI to send to</param>
        /// <param name="method">HTTP Method to use, defaults to <c>GET</c></param>
        /// <param name="message">Optional request payload, will be JSON-serialized</param>
        /// <param name="expectedStatuses">Any HTTP response statuses that can be interpretted
        ///         as successful, defaults to <c>OK (200)</c>; other response statuses will
        ///         trigger an exception; you can also skip response status checking by supplying
        ///         a zero-length array value here</param>
        /// <param name="skipNonce">If true, will not expect and extract a Nonce header in the
        ///         response, defaults to <c>false</c></param>
        /// <param name="skipSigning">If true, will not sign the request with the associated
        ///         Account key, defaults to <c>false</c></param>
        /// <param name="includePublicKey">If true, will include the Account's public key in the
        ///         payload signature instead of the Account's key ID as prescribed with certain
        ///         ACME protocol messages, defaults to <c>false</c></param>
        /// <param name="cancel">Optional cancellation token</param>
        /// <param name="opName">Name of operation, will be auto-populated with calling method
        ///         name if unspecified</param>
        /// <returns>The returned HTTP response message, unaltered, after inspecting the
        ///         response details for possible error or problem result</returns>
        async Task<HttpResponseMessage> SendAcmeAsync(
            Uri uri, HttpMethod method = null, object message = null,
            HttpStatusCode[] expectedStatuses = null,
            bool skipNonce = false, bool skipSigning = false, bool includePublicKey = false,
            CancellationToken cancel = default(CancellationToken),
            [System.Runtime.CompilerServices.CallerMemberName]string opName = "")
        {
            if (method == null)
                method = HttpMethod.Get;
            if (expectedStatuses == null)
                expectedStatuses = new[] { HttpStatusCode.OK };

            BeforeAcmeSign?.Invoke(opName, message);
            var requ = new HttpRequestMessage(method, uri);
            if (message != null)
            {
                string payload;
                if (skipSigning)
                    payload = ResolvePayload(message);
                else
                    payload = ComputeAcmeSigned(message, uri.ToString(),
                            includePublicKey: includePublicKey);
                requ.Content = new StringContent(payload);
                requ.Content.Headers.ContentType = Constants.JsonContentTypeHeaderValue;
            }

            BeforeHttpSend?.Invoke(opName, requ);
            var resp = await _http.SendAsync(requ);
            AfterHttpSend?.Invoke(opName, resp);

            if (!skipNonce)
                ExtractNextNonce(resp);

            if (expectedStatuses.Length > 0
                && !expectedStatuses.Contains(resp.StatusCode))
            {
                throw await DecodeResponseErrorAsync(resp, opName: opName);
            }
            
            return resp;
        }

        /// <summary>
        /// Convenience variation of <see cref="SendAcmeAsync"/> that deserializes
        /// and returns an expected type from the response content JSON.
        /// </summary>
        /// <remarks>
        /// All parameter semantics work as described in <see cref="SendAcmeAsync"/>.
        /// </remarks>
        async Task<T> SendAcmeAsync<T>(
            Uri uri, HttpMethod method = null, object message = null,
            HttpStatusCode[] expectedStatuses = null,
            bool skipNonce = false, bool skipSigning = false, bool includePublicKey = false,
            CancellationToken cancel = default(CancellationToken),
            [System.Runtime.CompilerServices.CallerMemberName]string opName = "")
        {
            return await Deserialize<T>(await SendAcmeAsync(
                    uri, method, message, expectedStatuses,
                    skipNonce, skipSigning, includePublicKey,
                    cancel, opName));
        }

        async Task<T> Deserialize<T>(HttpResponseMessage resp)
        {
            return JsonConvert.DeserializeObject<T>(
                    await resp.Content.ReadAsStringAsync());
        }

        async Task<AcmeProtocolException> DecodeResponseErrorAsync(HttpResponseMessage resp,
            string message = null,
            [System.Runtime.CompilerServices.CallerMemberName]string opName = "")
        {
            string msg = null;
            Problem problem = null;

            // if (Constants.ProblemContentTypeHeaderValue.Equals(resp.Content?.Headers?.ContentType))
            if (Constants.ProblemContentTypeHeaderValue.Equals(resp.Content?.Headers?.ContentType))
            {
                problem = await Deserialize<Problem>(resp);
                msg = problem.Detail;
            }

            if (string.IsNullOrEmpty(msg))
            {
                if (opName.EndsWith("Async"))
                    opName.Substring(0, opName.Length - "Async".Length);
                msg = $"Unexpected response status code [{resp.StatusCode}] for [{opName}]";
            }
            return new AcmeProtocolException(message ?? msg, problem);
        }

        /// <summary>
        /// Decodes an HTTP response, including the JSON payload and the ancillary HTTP data,
        /// into Account details.
        /// </summary>
        /// <param name="resp"></param>
        /// <param name="existing">Optionally, provide a previously decoded Account object
        ///         whose elements will be re-used as necessary to populate the new result
        ///         Account object; some ACME Account operations do not return the full
        ///         details of an existing Account</param>
        /// <returns></returns>
        protected async Task<AccountDetails> DecodeAccountResponseAsync(HttpResponseMessage resp,
            AccountDetails existing = null)
        {
            resp.Headers.TryGetValues("Link", out var linkValues);
            var acctUrl = resp.Headers.Location?.ToString();
            var links = new HTTP.LinkCollection(linkValues); // This allows/handles null
            var tosLink = links.GetFirstOrDefault(Constants.TosLinkHeaderRelationKey)?.Uri;

            // If this is a response to "duplicate account" then the body
            // will be empty and this will produce a null which we have
            // to account for when we build up the AcmeAccount instance
            var typedResp = await Deserialize<Account>(resp);

            // caResp will be null if this
            // is a duplicate account resp
            var acct = new AccountDetails
            {
                Payload = typedResp,
                Kid = acctUrl ?? existing?.Kid,
                TosLink = tosLink ?? existing?.TosLink,
            };
            
            return acct;
        }

        protected async Task<OrderDetails> DecodeOrderResponseAsync(HttpResponseMessage resp,
            OrderDetails existing = null)
        {
            var orderUrl = resp.Headers.Location?.ToString();
            var typedResponse = await Deserialize<Order>(resp);

            var order = new OrderDetails
            {
                Payload = typedResponse,
                OrderUrl = orderUrl ?? existing?.OrderUrl,
            };

            return order;

            // var order = new AcmeOrder
            // {
            //     OrderUrl = resp.Headers.Location?.ToString(),
            //     Status = coResp.Status,
            //     Expires = coResp.Expires == null
            //         ? DateTime.MinValue
            //         : DateTime.Parse(coResp.Expires),
            //     DnsIdentifiers = coResp.Identifiers.Select(x => x.Value).ToArray(),
            //     Authorizations = coResp.Authorizations.Select(x =>
            //             new AcmeAuthorization { DetailsUrl = x }).ToArray(),
            //     FinalizeUrl = coResp.Finalize,
            // };

            // foreach (var authz in order.Authorizations)
            // {
            //     resp = await _http.GetAsync(authz.DetailsUrl);
            //     var body = await resp.Content.ReadAsStringAsync();

            //     if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(body))
            //     {
            //         authz.FetchError = $"Failed to retrieve details: {resp.StatusCode}";
            //         if (resp.Content != null)
            //         {
            //             authz.FetchError += $"; {body}";
            //         }
            //     }
            //     else
            //     {
            //         authz.Details = JsonConvert
            //             .DeserializeObject<Protocol.Model.Authorization>(body);
            //     }
            // }

            // return order;
        }

        protected void ExtractNextNonce(HttpResponseMessage resp)
        {
            var headerName = Constants.ReplayNonceHeaderName;
            if (resp.Headers.TryGetValues(headerName, out var values))
            {
                NextNonce = string.Join(",", values);
            }
            else
            {
                throw new Exception($"missing header:  {headerName}");
            }
        }

        /// <summary>
        /// Computes the JWS-signed ACME request body for the given message object
        /// and the current or input <see cref="Signer"/>.
        /// </summary>
        protected string ComputeAcmeSigned(object message, string requUrl,
            IJwsTool signer = null,
            bool includePublicKey = false,
            bool excludeNonce = false)
        {
            if (signer == null)
                signer = Signer;

            var protectedHeader = new Dictionary<string, object>
            {
                ["alg"] = signer.JwsAlg,
                ["url"] = requUrl,
            };
            if (!excludeNonce)
                protectedHeader["nonce"] = NextNonce;

            if (includePublicKey)
                protectedHeader["jwk"] = signer.ExportJwk();
            else
                protectedHeader["kid"] = Account.Kid;


            // Nothing unprotected for now
            var unprotectedHeader = (object)null; // new { };

            var payload = ResolvePayload(message);
            var acmeSigned = JwsHelper.SignFlatJson(signer.Sign, payload,
                    protectedHeader, unprotectedHeader);

            return acmeSigned;
        }

        protected string ResolvePayload(object message)
        {
            var payload = string.Empty;
            if (message is string)
                payload = (string)message;
            else if (message is JObject)
                payload = ((JObject)message).ToString(Formatting.None);
            else
                payload = JsonConvert.SerializeObject(message, Formatting.None);
            return payload;           
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_disposeHttpClient)
                        _http?.Dispose();
                    _http = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AcmeClient() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}