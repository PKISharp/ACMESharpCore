using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
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
    public class AcmeOrderTests : IntegrationTest,
        IClassFixture<StateFixture>,
        IClassFixture<ClientsFixture>,
        IClassFixture<AwsFixture>
    {
        public AcmeOrderTests(ITestOutputHelper output, StateFixture state, ClientsFixture clients, AwsFixture aws)
            : base(state, clients)
        {
            Output = output;
            Aws = aws;
            Log = State.Factory.CreateLogger(typeof(AcmeOrderTests).FullName);
        }

        // https://xunit.github.io/docs/capturing-output
        // Will only be displayed if containing test fails.
        ITestOutputHelper Output { get; }

        AwsFixture Aws { get; }

        ILogger Log { get; }

        public static readonly IEnumerable<string> _contacts =
                new[] { "mailto:foo@example.com" };
        
        public static readonly IEnumerable<string> _dnsNames = new[] {
            "foo1.acme2.zyborg.io",
            "foo2.acme2.zyborg.io",
        };

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

        [Fact]
        [TestOrder(0_020)]
        public async Task TestCreateSan1Order()
        {
            var tctx = SetTestContext();

            var order = await Clients.Acme.CreateOrderAsync(_dnsNames);
            SaveObject("order-san1.json", order);
        }

        [Fact]
        [TestOrder(0_030)]
        public async Task TestCreateSan1OrderDuplicate()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-san1.json");

            Assert.NotNull(oldOrder);
            Assert.NotNull(oldOrder.OrderUrl);

            var newOrder = await Clients.Acme.CreateOrderAsync(_dnsNames);
            SaveObject("order-san1-dup.json", newOrder);

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

        [Fact]
        [TestOrder(0_040)]
        public void TestDecodeOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-san1.json");

            Assert.NotNull(oldOrder);
            Assert.NotNull(oldOrder.OrderUrl);

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = Clients.Acme.ResolveChallengeForDns01(authz, chlng);
                    SaveObject($"order-san1-authz_{authzIndex}-chlng_{chlngIndex}.json", chlngDetails);
                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_050)]
        public async Task TestAwsR53Call()
        {
            var tctx = SetTestContext();

            var rng = new Random();
            var dat = new byte[5];
            rng.NextBytes(dat);

            var name = "AcmeSharpCore.zyborg.io";
            var val = "TEST: " + BitConverter.ToString(dat);

            await Aws.R53.EditTxtRecord(name, new[] { val });

            Log.LogInformation($"Setting DNS Value to [{val}]");

            var maxTry = 10;
            var trySleep = 10 * 1000;

            for (var tryCount = 0; tryCount < maxTry; ++tryCount)
            {
                var x = await Clients.Dns.QueryAsync(name, QueryType.TXT);
                
                if (x.HasError)
                {
                    Log.LogInformation("Query DNS Error: " + x.ErrorMessage);
                }
                else
                {
                    var dnsVal = x.AllRecords?.FirstOrDefault()?.RecordToString().Trim('"');
                    if (val.Equals(dnsVal))
                    {
                        Log.LogInformation("Found expected DNS Value!");
                        return;
                    }

                    Log.LogInformation($"  Try #{tryCount} - found unexpected value: [{dnsVal}]");
                }

                Thread.Sleep(trySleep);
            }

            Assert.True(false, "Failed DNS set/read expected TXT record");
        }
    }
}
