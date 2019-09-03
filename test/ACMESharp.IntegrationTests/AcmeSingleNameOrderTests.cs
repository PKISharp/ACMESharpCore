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
    [TestOrder(0_10)]
    public class AcmeSingleNameOrderTests : AcmeOrderTests
    {
        public AcmeSingleNameOrderTests(ITestOutputHelper output,
                StateFixture state, ClientsFixture clients, AwsFixture aws)
            : base(output, state, clients, aws,
                    state.Factory.CreateLogger(typeof(AcmeSingleNameOrderTests).FullName))
        { }

        [Fact]
        [TestOrder(0_210, "SingleHttp")]
        public async Task Test_Create_Order_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var dnsNames = new[] {
                TestHttpSubdomain,
            };
            testCtx.GroupSaveObject("order_names.json", dnsNames);
            Log.LogInformation("Using Test DNS subdomain name: {0}", dnsNames);

            var order = await Clients.Acme.CreateOrderAsync(dnsNames);
            testCtx.GroupSaveObject("order.json", order);

            var authzDetails = order.Payload.Authorizations.Select(x =>
                Clients.Acme.GetAuthorizationDetailsAsync(x).GetAwaiter().GetResult());
            testCtx.GroupSaveObject("order-authz.json", authzDetails);
        }

        [Fact]
        [TestOrder(0_215, "SingleHttp")]
        public async Task Test_Create_OrderDuplicate_ForSingleHttp()
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
        [TestOrder(0_220, "SingleHttp")]
        public void Test_Decode_OrderChallengeForHttp01_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Http01ChallengeValidationDetails.Http01ChallengeType))
                {
                    Log.LogInformation("Decoding Authorization {0} Challenge {1}",
                            authzIndex, chlngIndex);
                    
                    var chlngDetails = AuthorizationDecoder.ResolveChallengeForHttp01(
                            authz, chlng, Clients.Acme.Signer);

                    Assert.Equal(Http01ChallengeValidationDetails.Http01ChallengeType,
                            chlngDetails.ChallengeType, ignoreCase: true);
                    Assert.NotNull(chlngDetails.HttpResourceUrl);
                    Assert.NotNull(chlngDetails.HttpResourcePath);
                    Assert.NotNull(chlngDetails.HttpResourceContentType);
                    Assert.NotNull(chlngDetails.HttpResourceValue);

                    testCtx.GroupSaveObject($"order-authz_{authzIndex}-chlng_{chlngIndex}.json",
                            chlngDetails);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_230, "SingleHttp")]
        public async Task Test_Create_OrderAnswerHttpContent_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Http01ChallengeValidationDetails.Http01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Http01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Creating HTTP Content for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);

                    await Aws.S3.EditFile(
                            chlngDetails.HttpResourcePath,
                            chlngDetails.HttpResourceContentType,
                            chlngDetails.HttpResourceValue);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_235, "SingleHttp")]
        public async Task Test_Exists_OrderAnswerHttpContent_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            Thread.Sleep(1*1000);

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Http01ChallengeValidationDetails.Http01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Http01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Waiting on HTTP content for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
 
                    var created = await ValidateHttpContent(chlngDetails.HttpResourceUrl,
                            contentType: chlngDetails.HttpResourceContentType,
                            targetValue: chlngDetails.HttpResourceValue);

                    Assert.True(created, "    Failed HTTP set/read expected content");

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_240, "SingleHttp")]
        public async Task Test_Answer_OrderChallenges_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Http01ChallengeValidationDetails.Http01ChallengeType))
                {
                    Log.LogInformation("Answering Authorization {0} Challenge {1}", authzIndex, chlngIndex);
                    var updated = await Clients.Acme.AnswerChallengeAsync(chlng.Url);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_245, "SingleHttp")]
        public async Task Test_AreValid_OrderChallengesAndAuthorization_ForSingleHttp()
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
                    x => x.Type == Http01ChallengeValidationDetails.Http01ChallengeType))
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
        // [TestOrder(0_250, "SingleHttp")]
        // public async Task Test_IsValid_OrderStatus_ForSingleHttp()
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
        [TestOrder(0_260, "SingleHttp")]
        public async Task Test_Finalize_Order_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");

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

            var certBytes = await Clients.Http.GetByteArrayAsync(updatedOrder.Payload.Certificate);
            testCtx.GroupWriteTo("order-cert.crt", certBytes);
        }

        [Fact]
        [TestOrder(0_270, "SingleHttp")]
        public async Task Test_Delete_OrderAnswerHttpContent_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Http01ChallengeValidationDetails.Http01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Http01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Deleting HTTP content for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);

                    await Aws.S3.EditFile(
                            chlngDetails.HttpResourcePath,
                            null, null);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }
        
        [Fact]
        [TestOrder(0_275, "SingleHttp")]
        public async Task Test_IsDeleted_OrderAnswerHttpContent_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            var oldOrder = testCtx.GroupLoadObject<OrderDetails>("order.json");
            var oldAuthz = testCtx.GroupLoadObject<Authorization[]>("order-authz.json");

            Thread.Sleep(1*1000);

            var authzIndex = 0;
            foreach (var authz in oldAuthz)
            {
                var chlngIndex = 0;
                foreach (var chlng in authz.Challenges.Where(
                    x => x.Type == Http01ChallengeValidationDetails.Http01ChallengeType))
                {
                    var chlngDetails = testCtx.GroupLoadObject<Http01ChallengeValidationDetails>(
                            $"order-authz_{authzIndex}-chlng_{chlngIndex}.json");

                    Log.LogInformation("Waiting on HTTP content deleted for Authorization {0} Challenge {1} as per {@Details}",
                            authzIndex, chlngIndex, chlngDetails);
 
                    var deleted = await ValidateHttpContent(chlngDetails.HttpResourceUrl,
                            targetMissing: true);

                    Assert.True(deleted, "Failed HTTP content delete/read expected missing TXT record: "
                            + chlngDetails.HttpResourceUrl);

                    ++chlngIndex;
                }
                ++authzIndex;
            }
        }

        [Fact]
        [TestOrder(0_280, "SingleHttp")]
        public async Task Test_Revoke_Certificate_ForSingleHttp()
        {
            var testCtx = SetTestContext();

            testCtx.GroupReadFrom("order-cert.crt", out var certPemBytes);
            var cert = new X509Certificate2(certPemBytes);
            var certDerBytes = cert.Export(X509ContentType.Cert);

            await Clients.Acme.RevokeCertificateAsync(
                certDerBytes, RevokeReason.Superseded);
        }

        [Fact]
        [TestOrder(0_285, "SingleHttp")]
        public async Task Test_Revoke_RevokedCertificate_ForSingleHttp()
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