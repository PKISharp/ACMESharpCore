using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static Random Rng { get; } = new Random();

        public static readonly IEnumerable<string> _contacts =
                new[] { "mailto:foo@example.com" };
        
        public const string TestSubdomain = "integtests.acme2.zyborg.io";

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
        [TestOrder(0_110)]
        public async Task TestCreateSingleNameOrder()
        {
            var tctx = SetTestContext();

            var randomNames = new[] {
                $"{State.RandomBytesString(5)}.{TestSubdomain}"
            };
            SaveObject("order_names-single.json", randomNames);
            Log.LogInformation("Generated random DNS name: {0}", randomNames);

            var order = await Clients.Acme.CreateOrderAsync(randomNames);
            SaveObject("order-single.json", order);
        }

        [Fact]
        [TestOrder(0_115)]
        public async Task TestCreateSingleNameOrderDuplicate()
        {
            var tctx = SetTestContext();

            var oldNames = LoadObject<string[]>("order_names-single.json");
            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            Assert.NotNull(oldNames);
            Assert.Equal(1, oldNames.Length);
            Assert.NotNull(oldOrder);
            Assert.NotNull(oldOrder.OrderUrl);

            var newOrder = await Clients.Acme.CreateOrderAsync(oldNames);
            SaveObject("order-single-dup.json", newOrder);

            ValidateDuplicateOrder(oldOrder, newOrder);
        }

        [Fact]
        [TestOrder(0_120)]
        public void TestDecodeDns01SingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    Log.LogInformation("Decoding Authorization {0} Challenge {1}",
                            authzIndex, chlngIndex);
                    
                    var chlngDetails = AuthorizationDecoder.ResolveChallengeForDns01(
                            authz, chlng, Clients.Acme.Signer);

                    Assert.Equal(Dns01ChallengeValidationDetails.Dns01ChallengeType,
                            chlngDetails.ChallengeType, ignoreCase: true);
                    Assert.NotNull(chlngDetails.DnsRecordName);
                    Assert.NotNull(chlngDetails.DnsRecordValue);
                    Assert.Equal("TXT", chlngDetails.DnsRecordType, ignoreCase: true);

                    SaveObject($"order-single-authz_{authzIndex}-chlng_{chlngIndex}.json",
                            chlngDetails);
                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_130)]
        public async Task TestCreateDnsRecordsForSingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = LoadObject<Dns01ChallengeValidationDetails>(
                            $"order-single-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Creating DNS for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
                    await Aws.R53.EditTxtRecord(
                            chlngDetails.DnsRecordName,
                            new[] { chlngDetails.DnsRecordValue });
                }
            }
        }

        [Fact]
        [TestOrder(0_135)]
        public async Task TestDnsRecordsExistForSingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            Thread.Sleep(10*1000);

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = LoadObject<Dns01ChallengeValidationDetails>(
                            $"order-single-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Waiting on DNS record for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
 
                    var created = await ValidateDnsTxtRecord(chlngDetails.DnsRecordName,
                            targetValue: chlngDetails.DnsRecordValue);

                    Assert.True(created, "    Failed DNS set/read expected TXT record");
                }
            }
        }

        [Fact]
        [TestOrder(0_140)]
        public async Task TestAnswerChallengesForSingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    Log.LogInformation("Answering Authorization {0} Challenge {1}", authzIndex, chlngIndex);
                    var updated = await Clients.Acme.AnswerChallengeAsync(authz, chlng);
                }
            }
        }

        [Fact]
        [TestOrder(0_145)]
        public async Task TestChallengesAndAuthorizationValidatedForSingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    int maxTry = 20;
                    int trySleep = 5 * 1000;
                    
                    for (var tryCount = 0; tryCount < maxTry; ++tryCount)
                    {
                        if (tryCount > 0)
                            // Wait just a bit for
                            // subsequent queries
                            Thread.Sleep(trySleep);

                        var updatedChlng = await Clients.Acme.RefreshChallengeAsync(authz, chlng);

                        // The Challenge is either Valid, still Pending or some other UNEXPECTED state

                        if ("valid" == updatedChlng.Status)
                        {
                            Log.LogInformation("    Authorization {0} Challenge {1} is VALID!", authzIndex, chlngIndex);
                            break;
                        }

                        if ("pending" != updatedChlng.Status)
                        {
                            Log.LogInformation("    Authorization {0} Challenge {1} in **UNEXPECTED STATUS**: {@UpdateChallengeDetails}",
                                    authzIndex, chlngIndex, updatedChlng);
                            throw new InvalidOperationException("Unexpected status for answered Challenge: " + updatedChlng.Status);
                        }
                    }
                }

                var updatedAuthz = await Clients.Acme.RefreshAuthorizationAsync(authz);
                Assert.Equal("valid", updatedAuthz.Details.Status);
            }
        }

        // [Fact]
        // [TestOrder(0_150)]
        // public async Task TestValidStatusForSingleNameOrder()
        // {
        //     var tctx = SetTestContext();

        //     // TODO: Validate overall order status is "valid"

        //     // This state is expected based on the ACME spec
        //     // BUT -- LE's implementation does not appear to
        //     // respect this contract -- the status of the
        //     // Order stays in the pending state even though
        //     // we are able to successfully Finalize the Order
        // }

        [Fact]
        [TestOrder(0_160)]
        public async Task TestFinalizeSingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            var rsaKeys = CryptoHelper.GenerateRsaKeys(4096);
            var rsa = CryptoHelper.GenerateRsaAlgorithm(rsaKeys);
            WriteTo("order-single-csr-keys.txt", rsaKeys);
            var derEncodedCsr = CryptoHelper.GenerateCsr(oldOrder.DnsIdentifiers, rsa);
            WriteTo("order-single-csr.der", derEncodedCsr);

            var updatedOrder = await Clients.Acme.FinalizeOrderAsync(oldOrder, derEncodedCsr);

            int maxTry = 20;
            int trySleep = 5 * 1000;
            var valid = false;

            for (var tryCount = 0; tryCount < maxTry; ++tryCount)
            {
                if (tryCount > 0)
                {
                    // Wait just a bit for
                    // subsequent queries
                    Thread.Sleep(trySleep);

                    // Only need to refresh
                    // after the first check
                    Log.LogInformation($"  Retry #{tryCount} refreshing Order");
                    updatedOrder = await Clients.Acme.RefreshOrderAsync(oldOrder);
                }

                if (!valid)
                {
                    // The Order is either Valid, still Pending or some other UNEXPECTED state

                    if ("valid" == updatedOrder.Status)
                    {
                        valid = true;
                        Log.LogInformation("Order is VALID!");
                    }
                    else if ("pending" != updatedOrder.Status)
                    {
                        Log.LogInformation("Order in **UNEXPECTED STATUS**: {@UpdateChallengeDetails}", updatedOrder);
                        throw new InvalidOperationException("Unexpected status for Order: " + updatedOrder.Status);
                    }
                }

                if (valid)
                {
                    // Once it's valid, then we need to wait for the Cert
                    
                    if (!string.IsNullOrEmpty(updatedOrder.CertificateUrl))
                    {
                        Log.LogInformation("Certificate URL is ready!");
                        break;
                    }
                }
            }

            Assert.NotNull(updatedOrder.CertificateUrl);

            var certBytes = await Clients.Http.GetByteArrayAsync(updatedOrder.CertificateUrl);
            WriteTo("order-single-cert.crt", certBytes);
        }

        [Fact]
        [TestOrder(0_170)]
        public async Task TestDeleteDnsRecordsForSingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = LoadObject<Dns01ChallengeValidationDetails>(
                            $"order-single-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Deleting DNS for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
                    await Aws.R53.EditTxtRecord(
                            chlngDetails.DnsRecordName,
                            new[] { chlngDetails.DnsRecordValue },
                            delete: true);
                }
            }
        }

        [Fact]
        [TestOrder(0_175)]
        public async Task TestDnsRecordsDeletedForSingleNameOrder()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-single.json");

            Thread.Sleep(10*1000);

            var authzIndex = 0;
            foreach (var authz in oldOrder.Authorizations)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Details.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = LoadObject<Dns01ChallengeValidationDetails>(
                            $"order-single-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Waiting on DNS record deleted for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
 
                    var deleted = await ValidateDnsTxtRecord(chlngDetails.DnsRecordName,
                            targetMissing: true);

                    Assert.True(deleted, "    Failed DNS delete/read expected missing TXT record");
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
                            Log.LogInformation($"    Try #{tryCount} - Found ANY DNS Value: {dnsVal}");
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


        // [Fact(Skip = "Skipping SAN tests for now")]
        [TestOrder(0_050)]
        public async Task TestCreateSan1OrderDuplicate()
        {
            var tctx = SetTestContext();

            var oldOrder = LoadObject<AcmeOrder>("order-san1.json");

            Assert.NotNull(oldOrder);
            Assert.NotNull(oldOrder.OrderUrl);

            var newOrder = await Clients.Acme.CreateOrderAsync(null); //_dnsNames);
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

        // [Fact(Skip = "Skipping SAN tests for now")]
        [TestOrder(0_060)]
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

        // [Fact(Skip = "Skipping AWS R53 test")]
        [TestOrder(0_999)]
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
    }
}
