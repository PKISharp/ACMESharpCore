using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Messages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ACMESharp
{
    /// <summary>
    /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7
    /// </summary>
    public class AcmeClient
    {
        private HttpClient _http;

        private IJwsTool _signer;

        public AcmeClient(HttpClient http, DirectoryResponse dir = null, AcmeAccount acct = null, IJwsTool signer = null)
        {
            Init(http, dir, acct, signer);
        }

        public AcmeClient(Uri baseUri, DirectoryResponse dir = null, AcmeAccount acct = null, IJwsTool signer = null)
        {
            var http = new HttpClient
            {
                BaseAddress = baseUri,
            };
            Init(http, dir, acct, signer);
        }

        private void Init(HttpClient http, DirectoryResponse dir, AcmeAccount acct, IJwsTool signer)
        {
            _http = http;
            Directory = dir ?? new DirectoryResponse();

            Account = acct;

            // We default to ES256 signer
            Signer = signer ?? new Crypto.JOSE.Impl.ESJwsTool();
            Signer.Init();
        }

        /// <summary>
        /// A tool that can be used to JWS-sign request messages to the target ACME server.
        /// </summary>
        /// <remarks>
        /// If not specified during construction, a default signing tool with a new set of keys will
        /// be constructed of type ES256 (Elliptic Curve using the P-256 curve and a SHA256 hash).
        /// </remarks>
        public IJwsTool Signer { get; private set; }

        public DirectoryResponse Directory { get; set; }

        public AcmeAccount Account { get; set; }

        public string NextNonce { get; private set; }

        public Action<string, HttpRequestMessage> BeforeHttpSend { get; set; }

        public Action<string, HttpResponseMessage> AfterHttpSend { get; set; }

        public async Task<DirectoryResponse> GetDirectoryAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var requ = new HttpRequestMessage(HttpMethod.Get, Directory.Directory);
            
            BeforeHttpSend?.Invoke(nameof(GetDirectoryAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(GetDirectoryAsync), resp);

            var body = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DirectoryResponse>(body);
        }

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.2
        /// </summary>
        public async Task GetNonceAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var requ = new HttpRequestMessage(HttpMethod.Head, Directory.NewNonce);
            
            BeforeHttpSend?.Invoke(nameof(GetNonceAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(GetNonceAsync), resp);

            ExtractNextNonce(resp);
        }

        /// <summary>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
        /// </summary>
        public async Task<AcmeAccount> CreateAccountAsync(string[] contacts,
            bool termsOfServiceAgreed = false,
            object externalAccountBinding = null,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, Directory.NewAccount);
            var requData = new CreateAccountRequest
            {
                Contact = contacts,
                TermsOfServiceAgreed = termsOfServiceAgreed,
                ExternalAccountBinding = (JwsSignedPayload)externalAccountBinding,
            };


            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            var requPayload = JsonConvert.SerializeObject(requData, jsonSettings);


            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);

            BeforeHttpSend?.Invoke(nameof(CreateAccountAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(CreateAccountAsync), resp);

            ExtractNextNonce(resp);

            var caResp = JsonConvert.DeserializeObject<CreateAccountResponse>(await resp.Content.ReadAsStringAsync());
            var links = new HTTP.LinkCollection(resp.Headers.GetValues("Link"));

            var acct = new AcmeAccount
            {
                PublicKey = Signer.ExportJwk(),
                Contacts = contacts,
                Kid = resp.Headers.Location?.ToString(),
                TosLink = links.GetFirstOrDefault(Constants.TosLinkHeaderRelationKey)?.Uri,
                Id = caResp.Id,
            };
            
            if (string.IsNullOrEmpty(acct.Kid))
                throw new InvalidDataException(
                        "account creation response does not include Location header");

            return acct;
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
        /// Computes the JWS-signed ACME request body for the given message object and the current
        /// <see cref="#Signer"/>.
        /// </summary>
        protected string ComputeAcmeSigned(object message, string requUrl)
        {
            var protectedHeader = new
            {
                alg = Signer.JwsAlg,
                jwk = Signer.ExportJwk(),
                nonce = NextNonce,
                url = requUrl,
            };

            // Nothing unprotected for now
            var unprotectedHeader = (object)null; // new { };

            var payload = string.Empty;
            if (message is JObject)
                payload = ((JObject)message).ToString(Formatting.None);
            else
                payload = JsonConvert.SerializeObject(message, Formatting.None);

            var acmeSigned = JwsHelper.SignFlatJson(Signer.Sign, payload,
                    protectedHeader, unprotectedHeader);

            return acmeSigned;
        }

        public async Task OrderCertificateAsync(
            CancellationToken cancel = default(CancellationToken))
        { }

        public async Task GetOrderStatusAsync(
            CancellationToken cancel = default(CancellationToken))
        { }

        // public async Task FetchChallengesAsync(
        //     CancellationToken cancel = default(CancellationToken))
        // { }

        // public async Task AnswerChallengesAsync(
        //     CancellationToken cancel = default(CancellationToken))
        // { }

        // public async Task FinishCertificateOrderAsync(
        //     CancellationToken cancel = default(CancellationToken))
        // { }

        public async Task AuthorizeIdentifierAsync(
            CancellationToken cancel = default(CancellationToken))
        { }

        public async Task IssueCertificateAsync(
            CancellationToken cancel = default(CancellationToken))
        { }

        public async Task RevokeCertificateAsync(
            CancellationToken cancel = default(CancellationToken))
        { }
    }
}