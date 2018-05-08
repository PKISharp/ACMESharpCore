using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using ACMESharp.Crypto;
using ACMESharp.Testing.Xunit;
using DnsClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace ACMESharp.IntegrationTests
{
    [Collection(nameof(AcmeOrderTests))]
    [CollectionDefinition(nameof(AcmeOrderTests))]
    [TestOrder(0_10)]
    public abstract class AcmeOrderTests : IntegrationTest,
        IClassFixture<StateFixture>,
        IClassFixture<ClientsFixture>,
        IClassFixture<AwsFixture>
    {
        public AcmeOrderTests(ITestOutputHelper output,
                StateFixture state, ClientsFixture clients, AwsFixture aws, ILogger log = null)
            : base(state, clients)
        {
            Output = output;
            Aws = aws;
            Log = log ?? state.Factory.CreateLogger(typeof(AcmeOrderTests).FullName);
        }

        // https://xunit.github.io/docs/capturing-output
        // Will only be displayed if containing test fails.
        protected ITestOutputHelper Output { get; }

        protected AwsFixture Aws { get; }

        protected ILogger Log { get; }

        public static Random Rng { get; } = new Random();

        protected  static readonly IEnumerable<string> _contacts =
                new[] { "mailto:foo@example.com" };
        
        protected  const string TestDnsSubdomain = "integtests.acme2.zyborg.io";

        protected  const string TestHttpSubdomain = "acmetesting.zyborg.io";

        [Fact]
        [TestOrder(0)]
        public async Task InitAcmeClient()
        {
            var tctx = SetTestContext();

            Clients.BaseAddress = new Uri(Constants.LetsEncryptV2StagingEndpoint);
            Clients.Http = new HttpClient()
            {
                BaseAddress = Clients.BaseAddress,
            };

            var acct = LoadObject<AcmeAccount>("acct.json");
            var keys = LoadObject<string>("acct-keys.json");
            if (acct == null || string.IsNullOrEmpty(keys))
            {
                Log.LogInformation("Durable Account data does not exist -- CREATING");

                Clients.Acme = new AcmeClient(Clients.Http);
                SetTestContext(); // To update the ACME client's Before/After hooks
                await InitDirectoryAndNonce();
                acct = await Clients.Acme.CreateAccountAsync(_contacts, true);
                Clients.Acme.Account = acct;

                SaveObject("acct.json", acct);
                SaveObject("acct-keys.json", Clients.Acme.Signer.Export());

                Log.LogInformation("Account details persisted");
            }
            else
            {
                Log.LogInformation("Found existing persisted Account data -- LOADING");

                Clients.Acme = new AcmeClient(Clients.Http, acct: acct);
                Clients.Acme.Signer.Import(keys);
                SetTestContext(); // To update the ACME client's Before/After hooks
                await InitDirectoryAndNonce();

                Log.LogInformation("Account details restored");
            }
        }

        private async Task InitDirectoryAndNonce()
        {
            var dir = await Clients.Acme.GetDirectoryAsync();
            Clients.Acme.Directory = dir;
            await Clients.Acme.GetNonceAsync();
        }

        [Fact]
        [TestOrder(0_010)]
        public async Task TestAccount()
        {
            var tctx = SetTestContext();

            var check = await Clients.Acme.CheckAccountAsync();
            Assert.Equal(Clients.Acme.Account.Kid, check.Kid);
        }

        //
        // Shared utility code
        //

        protected void ValidateDuplicateOrder(AcmeOrder oldOrder, AcmeOrder newOrder)
        {
            Assert.Equal(oldOrder.OrderUrl, newOrder.OrderUrl);

            // TODO: (Maybe file a bug with Boulder or LE?)
            // For some reason, the initial order has a high precision Expires value
            // and the subsequent version truncates the milliseconds component so we
            // just eliminate that discrepency for this test for the time being...

            var oldOrderExpires = DateTime.Parse(oldOrder.Expires.ToString("yyyy-MM-ddTHH:mm:ss"));
            var newOrderExpires = DateTime.Parse(newOrder.Expires.ToString("yyyy-MM-ddTHH:mm:ss"));

            Log.LogWarning("Temporary reformating OldOrder Expires date:"
                    + " {0:yyyy-MM-ddTHH:mm:ss.fffffff zzz} -> {1:yyyy-MM-ddTHH:mm:ss.fffffff zzz}",
                    oldOrder.Expires, oldOrderExpires);
            Log.LogWarning("Temporary reformating NewOrder Expires date:"
                    + " {0:yyyy-MM-ddTHH:mm:ss.fffffff zzz} -> {1:yyyy-MM-ddTHH:mm:ss.fffffff zzz}",
                    newOrder.Expires, newOrderExpires);
            
            oldOrder.Expires = oldOrderExpires;
            newOrder.Expires = newOrderExpires;

            // The order of Authz and Challenges within is not guaranteed so
            // we need to sort them into a canoncal format before comparing
            Canonicalize(oldOrder);
            Canonicalize(newOrder);

            Assert.Equal(
                JsonConvert.SerializeObject(oldOrder),
                JsonConvert.SerializeObject(newOrder));

            /// Local function to put Order into a canonical sort order
            void Canonicalize(AcmeOrder order)
            {
                order.DnsIdentifiers = order.DnsIdentifiers.OrderBy(x => x).ToArray();
                order.Authorizations = order.Authorizations.OrderBy(x => x.DetailsUrl).ToArray();
                foreach (var authz in order.Authorizations)
                {
                    authz.Details.Challenges = authz.Details.Challenges.OrderBy(x => x.Type).ToArray();
                }
            }
        }

        protected async Task<bool> ValidateDnsTxtRecord(string name,
                string targetValue = null,
                bool targetMissing = false,
                int maxTry = 20,
                int trySleep = 10 * 1000)
        {
            for (var tryCount = 0; tryCount < maxTry; ++tryCount)
            {
                if (tryCount > 0)
                    // Wait just a bit for
                    // subsequent queries
                    Thread.Sleep(trySleep);
                
                var x = await Clients.Dns.QueryAsync(name, QueryType.TXT);

                if (x.HasError)
                {
                    Log.LogInformation($"    Try #{tryCount} Query DNS Error: [{x.ErrorMessage}]");
                    if ("Non-Existent Domain".Equals(x.ErrorMessage, StringComparison.OrdinalIgnoreCase))
                    {
                        if (targetMissing)
                            return true;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unhandled DNS response: " + x.ErrorMessage);
                    }
                }
                else
                {
                    var dnsVal = x.AllRecords?.FirstOrDefault()?.RecordToString().Trim('"');

                    if (!string.IsNullOrEmpty(dnsVal))
                    {
                        if (!targetMissing && targetValue == null)
                        {
                            Log.LogInformation($"    Try #{tryCount} - Found ANY DNS value: {dnsVal}");
                            return true;
                        }

                        if (targetValue?.Equals(dnsVal) ?? false)
                        {
                            Log.LogInformation($"    Try #{tryCount} - Found expected DNS value: {dnsVal}");
                            return true;
                        }

                        Log.LogInformation($"    Try #{tryCount} - Found non-matching DNS value: {dnsVal}");
                    }
                    else
                    {
                        Log.LogInformation($"    Try #{tryCount} - Found EMPTY DNS value");
                    }
                }
            }

            return false;
        }

        protected async Task<bool> ValidateHttpContent(string url,
                string contentType = null,
                string targetValue = null,
                bool targetMissing = false,
                int maxTry = 20,
                int trySleep = 10 * 1000)
        {
            for (var tryCount = 0; tryCount < maxTry; ++tryCount)
            {
                if (tryCount > 0)
                    // Wait just a bit for
                    // subsequent queries
                    Thread.Sleep(trySleep);
                
                var x = await Clients.Http.GetAsync(url);

                if (x.StatusCode != HttpStatusCode.OK)
                {
                    Log.LogInformation($"    Try #{tryCount} HTTP GET Error: [{x.StatusCode}]");
                    if (x.StatusCode == HttpStatusCode.NotFound)
                    {
                        if (targetMissing)
                            return true;
                    }
                    else
                    {
                        throw new InvalidOperationException("Unhandled HTTP response: " + x.StatusCode);
                    }
                }
                else
                {
                    var getContent = await x.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(getContent))
                    {
                        if (!targetMissing && targetValue == null)
                        {
                            Log.LogInformation($"    Try #{tryCount} - Found ANY HTTP content: {getContent}");
                            return true;
                        }

                        if (targetValue?.Equals(getContent) ?? false)
                        {
                            Log.LogInformation($"    Try #{tryCount} - Found expected HTTP content: {getContent}");
                            return true;
                        }

                        Log.LogInformation($"    Try #{tryCount} - Found non-matching HTTP content: {getContent}");
                    }
                    else
                    {
                        Log.LogInformation($"    Try #{tryCount} - Found EMPTY HTTP content");
                    }
                }
            }

            return false;
        }
    }
}
