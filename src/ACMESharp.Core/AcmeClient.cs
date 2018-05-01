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
using ACMESharp.Protocol;
using ACMESharp.Protocol.Messages;
using ACMESharp.Protocol.Model;
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
        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

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

        /// <summary>
        /// Retrieves the Directory object from the target ACME CA.  The Directory is used
        /// to help clients configure themselves with the right URLs for each ACME operation.
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.1.1
        /// </remarks>
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
            var requ = new HttpRequestMessage(HttpMethod.Head, Directory.NewNonce);
            
            BeforeHttpSend?.Invoke(nameof(GetNonceAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(GetNonceAsync), resp);

            ExtractNextNonce(resp);
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
        /// </remarks>
        public async Task<AcmeAccount> CreateAccountAsync(string[] contacts,
            bool termsOfServiceAgreed = false,
            object externalAccountBinding = null,
            bool throwOnExistingAccount = false,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, Directory.NewAccount);
            var requData = new CreateAccountRequest
            {
                Contact = contacts,
                TermsOfServiceAgreed = termsOfServiceAgreed,
                ExternalAccountBinding = (JwsSignedPayload)externalAccountBinding,
            };

            var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);

            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString(),
                    includePublicKey: true));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);

            BeforeHttpSend?.Invoke(nameof(CreateAccountAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(CreateAccountAsync), resp);

            ExtractNextNonce(resp);

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                if (throwOnExistingAccount)
                    throw new InvalidOperationException("Existing account public key found");
            }
            else if (resp.StatusCode != HttpStatusCode.Created)
            {
                throw new InvalidOperationException("Unexpected response code:  " + resp.StatusCode);
            }

            var acct = await DecodeAccountResponseAsync(resp);
            
            if (string.IsNullOrEmpty(acct.Kid))
                throw new InvalidDataException(
                        "Account creation response does not include Location header");

            return acct;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.1
        /// </remarks>
        public async Task<AcmeAccount> CheckAccountAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, Directory.NewAccount);
            var requData = new CheckAccountRequest();
            var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString(),
                    includePublicKey: true));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);
            
            BeforeHttpSend?.Invoke(nameof(CheckAccountAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(CheckAccountAsync), resp);
            
            ExtractNextNonce(resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Invalid or missing account");

            var acct = await DecodeAccountResponseAsync(resp);

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
        public async Task<AcmeAccount> UpdateAccountAsync(string[] contacts,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(Account.Kid);
            var requData = new UpdateAccountRequest
            {
                Contact = contacts,
            };

            var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);
            
            BeforeHttpSend?.Invoke(nameof(UpdateAccountAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(UpdateAccountAsync), resp);
            
            ExtractNextNonce(resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Unexpected response to account update");

            var acct = await DecodeAccountResponseAsync(resp);

            if (string.IsNullOrEmpty(acct.Kid))
                throw new InvalidDataException(
                        "Account update response does not include Location header");

            return acct;
        }

        // TODO: handle "Change of TOS" error response
        //    https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.4


        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.6
        /// </remarks>
        public async Task ChangeAccountKeyAsync(IJwsTool newSigner,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, Directory.KeyChange);
            var requData = new KeyChangeRequest
            {
                Account = Account.Kid,
                NewKey = newSigner.ExportJwk(),
            };
            var requPayload = ComputeAcmeSigned(requData, requUrl.ToString(),
                    signer: newSigner, includePublicKey: true, excludeNonce: true);
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requPayload, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);
            
            BeforeHttpSend?.Invoke(nameof(ChangeAccountKeyAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(ChangeAccountKeyAsync), resp);
            
            ExtractNextNonce(resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Failed to change account key");

            Signer = newSigner;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.7
        /// </remarks>
        public async Task<AcmeAccount> DeactivateAccountAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(Account.Kid);
            var requData = new DeactivateAccountRequest();
            var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);
            
            BeforeHttpSend?.Invoke(nameof(DeactivateAccountAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(DeactivateAccountAsync), resp);
            
            ExtractNextNonce(resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Unexpected response to account update");

            return await DecodeAccountResponseAsync(resp);
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4
        /// </remarks>
        public async Task<AcmeOrder> CreateOrderAsync(string[] dnsIdentifiers,
            DateTime? notBefore = null,
            DateTime? notAfter = null,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, Directory.NewOrder);
            var requData = new CreateOrderRequest
            {
                Identifiers = dnsIdentifiers.Select(x =>
                        new Identifier { Type = "dns", Value = x}).ToArray(),

                // TODO: deal with dates
                // NotBefore = notBefore?.ToString(),
                // NotAfter = notAfter?.ToString(),
            };

            var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);
            
            BeforeHttpSend?.Invoke(nameof(CreateOrderAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(CreateOrderAsync), resp);
            
            ExtractNextNonce(resp);

            if (resp.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException("Unexpected response to create order");

            var coResp = JsonConvert.DeserializeObject<OrderResponse>(
                    await resp.Content.ReadAsStringAsync());
            
            var order = new AcmeOrder
            {
                OrderUrl = resp.Headers.Location?.ToString(),
                Status = coResp.Status,
                Expires = coResp.Expires == null
                    ? DateTime.MinValue
                    : DateTime.Parse(coResp.Expires),
                DnsIdentifiers = coResp.Identifiers.Select(x => x.Value).ToArray(),
                Authorizations = coResp.Authorizations.Select(x =>
                        new AcmeAuthorization { DetailsUrl = x }).ToArray(),
                FinalizeUrl = coResp.Finalize,
            };

            foreach (var authz in order.Authorizations)
            {
                resp = await _http.GetAsync(authz.DetailsUrl);
                var body = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(body))
                {
                    authz.FetchError = $"Failed to retrieve details: {resp.StatusCode}";
                    if (resp.Content != null)
                    {
                        authz.FetchError += $"; {body}";
                    }
                }
                else
                {
                    authz.Details = JsonConvert
                        .DeserializeObject<Protocol.Model.Authorization>(body);
                }
            }

            return order;
        }


        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-8
        /// </remarks>
        public IChallengeValidationDetails DecodeChallengeValidation(AcmeAuthorization authz,
                string challengeType)
        {
            var challenge = authz.Details.Challenges.Where(x => x.Type == challengeType)
                    .FirstOrDefault();
            if (challenge == null)
            {
                throw new InvalidOperationException(
                        $"Challenge type [{challengeType}] not found for given Authorization");
            }

            switch (challengeType)
            {
                case "dns-01":
                    return ResolveChallengeForDns01(authz, challenge);
            }

            throw new NotImplementedException(
                    $"Unknown or unsupported Challenge type [{challengeType}]");
        }


        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-8.4
        /// </remarks>
        public Dns01ChallengeValidationDetails ResolveChallengeForDns01(AcmeAuthorization authz,
                Challenge challenge)
        {
            var keyAuthzDigested = JwsHelper.ComputeKeyAuthorizationDigest(Signer, challenge.Token);

            return new Dns01ChallengeValidationDetails
            {
                DnsRecordName = $@"{Dns01ChallengeValidationDetails.DnsRecordNamePrefix}.{
                        authz.Details.Identifier.Value}",
                DnsRecordType = Dns01ChallengeValidationDetails.DnsRecordTypeDefault,
                DnsRecordValue = keyAuthzDigested,
            };
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.1
        /// </remarks>
        public async Task<Challenge> AnswerChallengeAsync(AcmeAuthorization authz,
            Challenge challenge,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(challenge.Url);
            // TODO:  for now, none of the challenge types
            // take any input data to answer the challenge
            var requData = new { };
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);
            
            BeforeHttpSend?.Invoke(nameof(AnswerChallengeAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(AnswerChallengeAsync), resp);

            ExtractNextNonce(resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Unexpected response to answer authorization challenge");
            
            return JsonConvert.DeserializeObject<Challenge>(
                    await resp.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.1
        /// </remarks>
        public async Task<Challenge> RefreshChallengeAsync(AcmeAuthorization authz,
            Challenge challenge,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(challenge.Url);
            var requ = new HttpRequestMessage(HttpMethod.Get, requUrl);
            
            BeforeHttpSend?.Invoke(nameof(RefreshChallengeAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(RefreshChallengeAsync), resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Unexpected response to refresh authorization challenge");
            
            return JsonConvert.DeserializeObject<Challenge>(
                    await resp.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.1
        /// </remarks>
        public async Task<AcmeAuthorization> DeactivateAuthorizationAsync(
            AcmeAuthorization authz,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(authz.DetailsUrl);
            var requData = new DeactivateAuthorizationRequest();
            var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);

            BeforeHttpSend?.Invoke(nameof(DeactivateAuthorizationAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(DeactivateAuthorizationAsync), resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Unexpected response to refresh authorization challenge");
            
            var authzDetail = JsonConvert.DeserializeObject<Protocol.Model.Authorization>(
                    await resp.Content.ReadAsStringAsync());
            
            return new AcmeAuthorization
            {
                DetailsUrl = authz.DetailsUrl,
                Details = authzDetail,
            };
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4
        /// </remarks>
        public async Task<AcmeOrder> FinalizeOrderAsync(AcmeOrder order,
            byte[] derEncodedCsr,
            CancellationToken cancel = default(CancellationToken))
        {
            var requUrl = new Uri(_http.BaseAddress, order.FinalizeUrl);
            var requData = new FinalizeOrderRequest
            {
                Csr = CryptoHelper.Base64UrlEncode(derEncodedCsr),
            };

            var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
                    Constants.ContentTypeHeaderValue);
            
            BeforeHttpSend?.Invoke(nameof(FinalizeOrderAsync), requ);
            var resp = await _http.SendAsync(requ, cancel);
            AfterHttpSend?.Invoke(nameof(FinalizeOrderAsync), resp);
            
            ExtractNextNonce(resp);

            if (resp.StatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException("Unexpected response to finalize order");

            var coResp = JsonConvert.DeserializeObject<OrderResponse>(
                    await resp.Content.ReadAsStringAsync());
            
            var newOrder = new AcmeOrder
            {
                OrderUrl = resp.Headers.Location?.ToString() ?? order.OrderUrl,
                Status = coResp.Status,
                Expires = coResp.Expires == null
                    ? DateTime.MinValue
                    : DateTime.Parse(coResp.Expires),
                DnsIdentifiers = coResp.Identifiers?.Select(x => x.Value).ToArray(),
                Authorizations = coResp.Authorizations?.Select(x =>
                        new AcmeAuthorization { DetailsUrl = x }).ToArray(),
                FinalizeUrl = coResp.Finalize,
            };

            if (newOrder.DnsIdentifiers == null)
            {
                newOrder.DnsIdentifiers = order.DnsIdentifiers;
            }
            
            if (newOrder.Authorizations == null)
            {
                newOrder.Authorizations = order.Authorizations;
            }

            foreach (var authz in newOrder.Authorizations)
            {
                resp = await _http.GetAsync(authz.DetailsUrl);
                var body = await resp.Content.ReadAsStringAsync();

                if (resp.StatusCode != HttpStatusCode.OK || string.IsNullOrEmpty(body))
                {
                    authz.FetchError = $"Failed to retrieve details: {resp.StatusCode}";
                    if (resp.Content != null)
                    {
                        authz.FetchError += $"; {body}";
                    }
                }
                else
                {
                    authz.Details = JsonConvert
                        .DeserializeObject<Protocol.Model.Authorization>(body);
                }
            }

            return newOrder;
        }

        protected async Task<AcmeAccount> DecodeAccountResponseAsync(HttpResponseMessage resp)
        {
            resp.Headers.TryGetValues("Link", out var linkValues);
            var links = new HTTP.LinkCollection(linkValues);

            // If this is a response to "duplicate account" then the body
            // will be empty and this will produce a null which we have
            // to account for when we build up the AcmeAccount instance
            var caResp = JsonConvert.DeserializeObject<CreateAccountResponse>(
                    await resp.Content.ReadAsStringAsync());
            var acct = new AcmeAccount
            {
                Kid = resp.Headers.Location?.ToString() ?? Account.Kid,
                TosLink = links.GetFirstOrDefault(Constants.TosLinkHeaderRelationKey)?.Uri,

                // caResp will be null if this
                // is a duplicate account resp
                PublicKey = caResp?.Key,
                Contacts = caResp?.Contact,
                Id = caResp?.Id,
            };
            
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
        /// Computes the JWS-signed ACME request body for the given message object
        /// and the current or input <see cref="#Signer"/>.
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

            var payload = string.Empty;
            if (message is string)
                payload = (string)message;
            else if (message is JObject)
                payload = ((JObject)message).ToString(Formatting.None);
            else
                payload = JsonConvert.SerializeObject(message, Formatting.None);

            var acmeSigned = JwsHelper.SignFlatJson(signer.Sign, payload,
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