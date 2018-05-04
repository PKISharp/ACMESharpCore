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
    public class AcmeClient : IDisposable
    {
        private static readonly HttpStatusCode[] SkipExpectedStatuses = new HttpStatusCode[0];

        private static readonly JsonSerializerSettings _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private bool _disposeHttpClient;
        private HttpClient _http;

        private IJwsTool _signer;

        public AcmeClient(HttpClient http, DirectoryResponse dir = null,
                AcmeAccount acct = null, IJwsTool signer = null,
                bool disposeHttpClient = false)
        {
            Init(http, dir, acct, signer);
            _disposeHttpClient = disposeHttpClient;
        }

        public AcmeClient(Uri baseUri, DirectoryResponse dir = null,
                AcmeAccount acct = null, IJwsTool signer = null)
        {
            var http = new HttpClient
            {
                BaseAddress = baseUri,
            };
            Init(http, dir, acct, signer);
            _disposeHttpClient = true;
        }

        private void Init(HttpClient http, DirectoryResponse dir,
                AcmeAccount acct, IJwsTool signer)
        {
            _http = http;
            Directory = dir ?? new DirectoryResponse();

            Account = acct;

            // We default to ES256 signer
            Signer = signer ?? new Crypto.JOSE.Impl.ESJwsTool();
            Signer.Init();
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

        public DirectoryResponse Directory { get; set; }

        public AcmeAccount Account { get; set; }

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
        public async Task<DirectoryResponse> GetDirectoryAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            return await SendAcmeAsync<DirectoryResponse>(
                    new Uri(_http.BaseAddress, Directory.Directory),
                    skipNonce: true,
                    cancel: cancel);

            // // var requ = new HttpRequestMessage(HttpMethod.Get, Directory.Directory);
            
            // // BeforeHttpSend?.Invoke(nameof(GetDirectoryAsync), requ);
            // // var resp = await _http.SendAsync(requ, cancel);
            // // AfterHttpSend?.Invoke(nameof(GetDirectoryAsync), resp);

            // // var body = await resp.Content.ReadAsStringAsync();
            // // return JsonConvert.DeserializeObject<DirectoryResponse>(body);
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

            // // var requ = new HttpRequestMessage(HttpMethod.Head, Directory.NewNonce);
            
            // // BeforeHttpSend?.Invoke(nameof(GetNonceAsync), requ);
            // // var resp = await _http.SendAsync(requ, cancel);
            // // AfterHttpSend?.Invoke(nameof(GetNonceAsync), resp);

            // // ExtractNextNonce(resp);
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3
        /// </remarks>
        public async Task<AcmeAccount> CreateAccountAsync(IEnumerable<string> contacts,
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
            
            // var requUrl = new Uri(_http.BaseAddress, Directory.NewAccount);
            // var requData = new CreateAccountRequest
            // {
            //     Contact = contacts.ToArray(),
            //     TermsOfServiceAgreed = termsOfServiceAgreed,
            //     ExternalAccountBinding = (JwsSignedPayload)externalAccountBinding,
            // };

            // //!var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);

            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString(),
            //         includePublicKey: true));
            // requ.Content.Headers.ContentType = Constants.JsonContentTypeHeaderValue;

            // BeforeAcmeSign?.Invoke(nameof(CreateAccountAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(CreateAccountAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(CreateAccountAsync), resp);

            // ExtractNextNonce(resp);

            if (resp.StatusCode == HttpStatusCode.OK)
            {
                if (throwOnExistingAccount)
                    throw new InvalidOperationException("Existing account public key found");
            }
            // else if (resp.StatusCode != HttpStatusCode.Created)
            // {
            //     throw await DecodeResponseErrorAsync(resp);
            // }

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
        public async Task<AcmeAccount> CheckAccountAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, Directory.NewAccount),
                    method: HttpMethod.Post,
                    message: new CheckAccountRequest(),
                    expectedStatuses: SkipExpectedStatuses,
                    includePublicKey: true,
                    cancel: cancel);


            // var requUrl = new Uri(_http.BaseAddress, Directory.NewAccount);
            // var requData = new CheckAccountRequest();
            // //!var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString(),
            //         includePublicKey: true));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);
            
            // BeforeAcmeSign?.Invoke(nameof(CheckAccountAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(CheckAccountAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(CheckAccountAsync), resp);
            
            // ExtractNextNonce(resp);

            if (resp.StatusCode == HttpStatusCode.BadRequest)
                throw new InvalidOperationException(
                        $"Invalid or missing account ({resp.StatusCode})");

            if (resp.StatusCode != HttpStatusCode.OK)
                throw await DecodeResponseErrorAsync(resp);

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
        public async Task<AcmeAccount> UpdateAccountAsync(IEnumerable<string> contacts,
            CancellationToken cancel = default(CancellationToken))
        {
            var message = new UpdateAccountRequest
            {
                Contact = contacts,
            };
            var resp = await SendAcmeAsync(
                    new Uri(Account.Kid),
                    method: HttpMethod.Post,
                    message: message,
                    cancel: cancel);

            // var requUrl = new Uri(Account.Kid);
            // var requData = new UpdateAccountRequest
            // {
            //     Contact = contacts,
            // };

            // //!var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);
            
            // BeforeAcmeSign?.Invoke(nameof(UpdateAccountAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(UpdateAccountAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(UpdateAccountAsync), resp);
            
            // ExtractNextNonce(resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw new InvalidOperationException("Unexpected response to account update");

            var acct = await DecodeAccountResponseAsync(resp);

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
        public async Task<AcmeAccount> ChangeAccountKeyAsync(IJwsTool newSigner,
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

            // var requUrl = new Uri(_http.BaseAddress, Directory.KeyChange);
            // var requData = new KeyChangeRequest
            // {
            //     Account = Account.Kid,
            //     NewKey = newSigner.ExportJwk(),
            // };
            // var innerPayload = ComputeAcmeSigned(requData, requUrl.ToString(),
            //         signer: newSigner, includePublicKey: true, excludeNonce: true);
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(innerPayload, requUrl.ToString()));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);
            
            // BeforeAcmeSign?.Invoke(nameof(ChangeAccountKeyAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(ChangeAccountKeyAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(ChangeAccountKeyAsync), resp);
            
            // ExtractNextNonce(resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw new InvalidOperationException("Failed to change account key");

            Signer = newSigner;

            return await DecodeAccountResponseAsync(resp);
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.3.7
        /// </remarks>
        public async Task<AcmeAccount> DeactivateAccountAsync(
            CancellationToken cancel = default(CancellationToken))
        {
            var resp = await SendAcmeAsync(
                    new Uri(Account.Kid),
                    method: HttpMethod.Post,
                    message: new DeactivateAccountRequest(),
                    cancel: cancel);

            // var requUrl = new Uri(Account.Kid);
            // var requData = new DeactivateAccountRequest();
            // //!var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);
            
            // BeforeAcmeSign?.Invoke(nameof(DeactivateAccountAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(DeactivateAccountAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(DeactivateAccountAsync), resp);
            
            // ExtractNextNonce(resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw await DecodeResponseErrorAsync(resp);

            return await DecodeAccountResponseAsync(resp);
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.4
        /// </remarks>
        public async Task<AcmeOrder> CreateOrderAsync(IEnumerable<string> dnsIdentifiers,
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

            // var requUrl = new Uri(_http.BaseAddress, Directory.NewOrder);
            // var requData = new CreateOrderRequest
            // {
            //     Identifiers = dnsIdentifiers.Select(x =>
            //             new Identifier { Type = "dns", Value = x}).ToArray(),

            //     // TODO: deal with dates
            //     // NotBefore = notBefore?.ToString(),
            //     // NotAfter = notAfter?.ToString(),
            // };

            // //!var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);
            
            // BeforeAcmeSign?.Invoke(nameof(CreateOrderAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(CreateOrderAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(CreateOrderAsync), resp);
            
            // ExtractNextNonce(resp);

            // if (resp.StatusCode != HttpStatusCode.Created)
            //     throw new InvalidOperationException("Unexpected response to create order");

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
            var keyAuthzDigested = JwsHelper.ComputeKeyAuthorizationDigest(
                    Signer, challenge.Token);

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
            var resp = await SendAcmeAsync<Challenge>(
                    new Uri(challenge.Url),
                    method: HttpMethod.Post,
                    // TODO:  for now, none of the challenge types
                    // take any input data to answer the challenge
                    message: new { },
                    cancel: cancel);

            // var requUrl = new Uri(challenge.Url);
            // // TODO:  for now, none of the challenge types
            // // take any input data to answer the challenge
            // var requData = new { };
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);
            
            // BeforeAcmeSign?.Invoke(nameof(AnswerChallengeAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(AnswerChallengeAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(AnswerChallengeAsync), resp);

            // ExtractNextNonce(resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw new InvalidOperationException(
            //             "Unexpected response to answer authorization challenge");
            
            // return JsonConvert.DeserializeObject<Challenge>(
            //         await resp.Content.ReadAsStringAsync());
        
            return resp;
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
            var resp = await SendAcmeAsync<Challenge>(
                    new Uri(challenge.Url),
                    skipNonce: true,
                    cancel: cancel);

            // var requUrl = new Uri(challenge.Url);
            // var requ = new HttpRequestMessage(HttpMethod.Get, requUrl);
            
            // BeforeAcmeSign?.Invoke(nameof(RefreshChallengeAsync), null);
            // BeforeHttpSend?.Invoke(nameof(RefreshChallengeAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(RefreshChallengeAsync), resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw new InvalidOperationException(
            //             "Unexpected response to refresh authorization challenge");
            
            // return JsonConvert.DeserializeObject<Challenge>(
            //         await resp.Content.ReadAsStringAsync());

            return resp;
        }


        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-7.5.1
        /// </remarks>
        public async Task<AcmeAuthorization> RefreshAuthorizationAsync(AcmeAuthorization authz,
            CancellationToken cancel = default(CancellationToken))
        {
            var resp = await SendAcmeAsync<Protocol.Model.Authorization>(
                    new Uri(authz.DetailsUrl),
                    skipNonce: true,
                    cancel: cancel);

            // var requUrl = new Uri(challenge.Url);
            // var requ = new HttpRequestMessage(HttpMethod.Get, requUrl);
            
            // BeforeAcmeSign?.Invoke(nameof(RefreshChallengeAsync), null);
            // BeforeHttpSend?.Invoke(nameof(RefreshChallengeAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(RefreshChallengeAsync), resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw new InvalidOperationException(
            //             "Unexpected response to refresh authorization challenge");
            
            // return JsonConvert.DeserializeObject<Challenge>(
            //         await resp.Content.ReadAsStringAsync());

            return new AcmeAuthorization
            {
                DetailsUrl = authz.DetailsUrl,
                Details = resp,
            };
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
            var resp = await SendAcmeAsync<Protocol.Model.Authorization>(
                    new Uri(authz.DetailsUrl),
                    method: HttpMethod.Post,
                    message: new DeactivateAuthorizationRequest(),
                    cancel: cancel);

            // var requUrl = new Uri(authz.DetailsUrl);
            // var requData = new DeactivateAuthorizationRequest();
            // //!var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);

            // BeforeAcmeSign?.Invoke(nameof(DeactivateAuthorizationAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(DeactivateAuthorizationAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(DeactivateAuthorizationAsync), resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw new InvalidOperationException(
            //             "Unexpected response to refresh authorization challenge");
            
            // var authzDetail = JsonConvert.DeserializeObject<Protocol.Model.Authorization>(
            //         await resp.Content.ReadAsStringAsync());
            
            return new AcmeAuthorization
            {
                DetailsUrl = authz.DetailsUrl,
                Details = resp,
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
            var message = new FinalizeOrderRequest
            {
                Csr = CryptoHelper.Base64UrlEncode(derEncodedCsr),
            };
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, order.FinalizeUrl),
                    method: HttpMethod.Post,
                    message: message,
                    cancel: cancel);

            // var requUrl = new Uri(_http.BaseAddress, order.FinalizeUrl);
            // var requData = new FinalizeOrderRequest
            // {
            //     Csr = CryptoHelper.Base64UrlEncode(derEncodedCsr),
            // };

            // //!var requPayload = JsonConvert.SerializeObject(requData, _jsonSettings);
            // var requ = new HttpRequestMessage(HttpMethod.Post, requUrl);
            // requ.Content = new StringContent(ComputeAcmeSigned(requData, requUrl.ToString()));
            // requ.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(
            //         Constants.ContentTypeHeaderValue);
            
            // BeforeAcmeSign?.Invoke(nameof(FinalizeOrderAsync), requData);
            // BeforeHttpSend?.Invoke(nameof(FinalizeOrderAsync), requ);
            // var resp = await _http.SendAsync(requ, cancel);
            // AfterHttpSend?.Invoke(nameof(FinalizeOrderAsync), resp);
            
            // ExtractNextNonce(resp);

            // if (resp.StatusCode != HttpStatusCode.OK)
            //     throw new InvalidOperationException("Unexpected response to finalize order");

            var coResp = JsonConvert.DeserializeObject<OrderResponse>(
                    await resp.Content.ReadAsStringAsync());
            
            var newOrder = new AcmeOrder
            {
                OrderUrl = resp.Headers.Location?.ToString() ?? order.OrderUrl,
                Status = coResp.Status,
                Expires = coResp.Expires == null
                    ? DateTime.MinValue
                    : DateTime.Parse(coResp.Expires),
                DnsIdentifiers = coResp.Identifiers?.Select(x => x.Value).ToArray()
                    ?? order.DnsIdentifiers,
                Authorizations = coResp.Authorizations?.Select(x =>
                        new AcmeAuthorization { DetailsUrl = x }).ToArray()
                    ?? order.Authorizations,
                FinalizeUrl = coResp.Finalize,
            };

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

        public async Task<AcmeOrder> RefreshOrderAsync(AcmeOrder order,
            CancellationToken cancel = default(CancellationToken))
        {
            var resp = await SendAcmeAsync(
                    new Uri(_http.BaseAddress, order.OrderUrl),
                    skipNonce: true,
                    cancel: cancel);

            var coResp = JsonConvert.DeserializeObject<OrderResponse>(
                    await resp.Content.ReadAsStringAsync());
            
            var updatedDrder = new AcmeOrder
            {
                OrderUrl = resp.Headers.Location?.ToString() ?? order.OrderUrl,
                Status = coResp.Status,
                Expires = coResp.Expires == null
                    ? DateTime.MinValue
                    : DateTime.Parse(coResp.Expires),
                DnsIdentifiers = coResp.Identifiers?.Select(x => x.Value).ToArray()
                    ?? order.DnsIdentifiers,
                Authorizations = coResp.Authorizations?.Select(x =>
                        new AcmeAuthorization { DetailsUrl = x }).ToArray()
                    ?? order.Authorizations,
                FinalizeUrl = coResp.Finalize,
                CertificateUrl = coResp.Certificate,
            };

            foreach (var authz in updatedDrder.Authorizations)
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

            return updatedDrder;
        }

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
        /// Convenience variation of <see cref="#SendAcmeAsync"/> that deserializes
        /// and returns an expected type from the response content JSON.
        /// </summary>
        async Task<T> SendAcmeAsync<T>(
            Uri uri, HttpMethod method = null, object message = null,
            HttpStatusCode[] expectedStatuses = null,
            bool skipNonce = false, bool skipSigning = false, bool includePublicKey = false,
            CancellationToken cancel = default(CancellationToken),
            [System.Runtime.CompilerServices.CallerMemberName]string opName = "")
        {
            var resp = await SendAcmeAsync(
                    uri, method, message, expectedStatuses,
                    skipNonce, skipSigning, includePublicKey,
                    cancel, opName);

            var respObject = JsonConvert.DeserializeObject<T>(
                    await resp.Content.ReadAsStringAsync());

            return respObject;
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
                problem = JsonConvert.DeserializeObject<Problem>(
                        await resp.Content.ReadAsStringAsync());
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

                // caResp will be null if this
                // is a duplicate account resp
                TosLink = links.GetFirstOrDefault(Constants.TosLinkHeaderRelationKey)?.Uri,
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