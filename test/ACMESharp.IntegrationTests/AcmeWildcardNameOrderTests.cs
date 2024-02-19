using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using ACMESharp.Crypto;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using ACMESharp.Testing.Xunit;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace ACMESharp.IntegrationTests
{
    [Collection(nameof(AcmeOrderTests))]
    [CollectionDefinition(nameof(AcmeOrderTests))]
    [TestOrder(0_200)]
    public class AcmeWildcardNameOrderTests : AcmeOrderTests
    {
        public AcmeWildcardNameOrderTests(ITestOutputHelper output,
                StateFixture state, ClientsFixture clients, AwsFixture aws)
            : base(output, state, clients, aws,
                    state.Factory.CreateLogger(typeof(AcmeMultiNameOrderTests).FullName))
        { }

        [Fact]
        [TestOrder(0_110, "WildDns")]
        public async Task Test_Create_Order_ForWildDns()
        {
            var testCtx = SetTestContext();

            var dnsNames = new[] {
                $"*.{State.RandomBytesString(5)}.{TestDnsSubdomain}",
            };
            testCtx.GroupSaveObject("order_names.json", dnsNames);
            Log.LogInformation("Generated random DNS name: {0}", dnsNames);

            var order = await Clients.Acme.CreateOrderAsync(dnsNames);
            testCtx.GroupSaveObject("order.json", order);

            var authzDetails = order.Payload.Authorizations.Select(x =>
                Clients.Acme.GetAuthorizationDetailsAsync(x).GetAwaiter().GetResult());
            testCtx.GroupSaveObject("order-authz.json", authzDetails);

            Assert.True(authzDetails.First().Wildcard, "Is a wildcard Order");
        }

        [Fact]
        [TestOrder(0_115, "WildDns")]
        public async Task Test_Create_OrderDuplicate_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldNames = testCtx.GroupLoadObject<string[]>("order_names.json");
            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");

            Assert.NotNull(oldNames);
            Assert.Single(oldNames);
            Assert.NotNull(oldOrder);
            Assert.NotNull(oldOrder.OrderUrl);

            var newOrder = await Clients.Acme.CreateOrderAsync(oldNames);
            testCtx.GroupSaveObject("order-dup.json", newOrder);

            ValidateDuplicateOrder(oldOrder, newOrder);
        }

        [Fact]
        [TestOrder(0_120, "WildDns")]
        public void Test_Decode_OrderChallengeForDns01_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
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

                    testCtx.GroupSaveObject($"order-authz_{authzIndex}-chlng_{chlngIndex}.json",
                            chlngDetails);
                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_130, "WildDns")]
        public async Task Test_Create_OrderAnswerDnsRecords_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Dns01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Creating DNS for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
                    await Aws.R53.EditTxtRecord(
                            chlngDetails.DnsRecordName,
                            new[] { chlngDetails.DnsRecordValue });

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_135, "WildDns")]
        public async Task Test_Exist_OrderAnswerDnsRecords_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            Thread.Sleep(10*1000);

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Dns01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Waiting on DNS record for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
 
                    var created = await ValidateDnsTxtRecord(chlngDetails.DnsRecordName,
                            targetValue: chlngDetails.DnsRecordValue);

                    Assert.True(created, "    Failed DNS set/read expected TXT record");

                    ++chlngIndex;
                }
                ++authzIndex;
            }

            // We're adding an artificial wait here -- even though we were able to successfully
            // read the expected DNS record, in practice we found it's not always "universally"
            // available from all the R53 PoP servers, specifically from where LE STAGE queries
            Thread.Sleep(10 * 1000 * oldOrder.Payload.Identifiers.Length);
        }

        [Fact]
        [TestOrder(0_140, "WildDns")]
        public async Task Test_Answer_OrderChallenges_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    Log.LogInformation("Answering Authorization {0} Challenge {1}", authzIndex, chlngIndex);
                    var updated = await Clients.Acme.AnswerChallengeAsync(chlng.Url);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_145, "WildDns")]
        public async Task Test_AreValid_OrderChallengesAndAuthorization_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var authzUrl = oldOrder.Payload.Authorizations[authzIndex];
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
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

                        var updatedChlng = await Clients.Acme.GetChallengeDetailsAsync(chlng.Url);

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
                    ++chlngIndex;
                }
                ++authzIndex;

                var updatedAuthz = await Clients.Acme.GetAuthorizationDetailsAsync(authzUrl);
                Assert.Equal("valid", updatedAuthz.Status);
            }
        }

        // [Fact]
        // [TestOrder(0_150, "WildDns")]
        // public async Task TestValidStatusForSingleNameOrder()
        // {
        //     var testCtx = SetTestContext();

        //     // TODO: Validate overall order status is "valid"

        //     // This state is expected based on the ACME spec
        //     // BUT -- LE's implementation does not appear to
        //     // respect this contract -- the status of the
        //     // Order stays in the pending state even though
        //     // we are able to successfully Finalize the Order
        // }

        [Fact]
        [TestOrder(0_160, "WildDns")]
        public async Task Test_Finalize_Order_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var rsaKeys = CryptoHelper.Rsa.GenerateKeys(4096);
            var rsa = CryptoHelper.Rsa.GenerateAlgorithm(rsaKeys);
            testCtx.GroupWriteTo("order-csr-keys.txt", rsaKeys);
            var derEncodedCsr = CryptoHelper.Rsa.GenerateCsr(
                    oldOrder.Payload.Identifiers.Select(x => x.Value), rsa);
            testCtx.GroupWriteTo("order-csr.der", derEncodedCsr);

            var updatedOrder = await Clients.Acme.FinalizeOrderAsync(
                    oldOrder.Payload.Finalize, derEncodedCsr);

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
                    updatedOrder = await Clients.Acme.GetOrderDetailsAsync(oldOrder.OrderUrl);
                    testCtx.GroupSaveObject("order-updated.json", updatedOrder);
                }

                if (!valid)
                {
                    // The Order is either Valid, still Pending or some other UNEXPECTED state

                    if ("valid" == updatedOrder.Payload.Status)
                    {
                        valid = true;
                        Log.LogInformation("Order is VALID!");
                    }
                    else if ("pending" != updatedOrder.Payload.Status)
                    {
                        Log.LogInformation("Order in **UNEXPECTED STATUS**: {@UpdateChallengeDetails}", updatedOrder);
                        throw new InvalidOperationException("Unexpected status for Order: " + updatedOrder.Payload.Status);
                    }
                }

                if (valid)
                {
                    // Once it's valid, then we need to wait for the Cert
                    
                    if (!string.IsNullOrEmpty(updatedOrder.Payload.Certificate))
                    {
                        Log.LogInformation("Certificate URL is ready!");
                        break;
                    }
                }
            }

            Assert.NotNull(updatedOrder.Payload.Certificate);

            var certBytes = await Clients.Acme.GetByteArrayAsync(updatedOrder.Payload.Certificate);
            testCtx.GroupWriteTo("order-cert.crt", certBytes);
        }

        [Fact]
        [TestOrder(0_170, "WildDns")]
        public async Task Test_Delete_OrderAnswerDnsRecords_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Dns01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Deleting DNS for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
                    await Aws.R53.EditTxtRecord(
                            chlngDetails.DnsRecordName,
                            new[] { chlngDetails.DnsRecordValue },
                            delete: true);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_175, "WildDns")]
        public async Task Test_IsDeleted_OrderAnswerDnsRecords_ForWildDns()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            Thread.Sleep(10*1000);

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Dns01ChallengeValidationDetails.Dns01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Dns01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Waiting on DNS record deleted for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
 
                    var deleted = await ValidateDnsTxtRecord(chlngDetails.DnsRecordName,
                            targetMissing: true, trySleep: 20000);

                    Assert.True(deleted, "Failed DNS delete/read expected missing TXT record: "
                            + chlngDetails.DnsRecordName);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_180, "WildDns")]
        public async Task Test_Revoke_Certificate_ForWildDns()
        {
            var testCtx = SetTestContext();

            testCtx.GroupReadFrom("order-cert.crt", out var certPemBytes);
            var cert = new X509Certificate2(certPemBytes);
            var certDerBytes = cert.Export(X509ContentType.Cert);
            
            await Clients.Acme.RevokeCertificateAsync(
                certDerBytes, RevokeReason.Superseded);
        }

        [Fact]
        [TestOrder(0_185, "WildDns")]
        public async Task Test_Revoke_RevokedCertificate_ForWildDns()
        {
            var testCtx = SetTestContext();

            testCtx.GroupReadFrom("order-cert.crt", out var certPemBytes);
            var cert = new X509Certificate2(certPemBytes);
            var certDerBytes = cert.Export(X509ContentType.Cert);

            var ex = await Assert.ThrowsAsync<AcmeProtocolException>(
                async () => await Clients.Acme.RevokeCertificateAsync(
                    certDerBytes, RevokeReason.Superseded));

            Assert.StrictEqual(ProblemType.AlreadyRevoked, ex.ProblemType);
        }
    }
}