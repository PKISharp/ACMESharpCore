using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using ACMESharp.Protocol;
using System;
using ACMESharp.Protocol.Resources;
using System.IO;
using System.Collections.Generic;
using PKISharp.SimplePKI;
using System.Linq;

namespace ACMESharp.MockServer.UnitTests
{
    [TestClass]
    public class AcmeControllerTests
    {
        // When using the ASP.NET Core TestHost, only the URL Path is significant
        public const string DefaultServerUrl = "http://localhost/";
        public const string RepoFilePath = @".\_IGNORE\data\acme-mockserver.db";

        static TestServer _server;

        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            if (File.Exists(RepoFilePath))
                File.Delete(RepoFilePath);
            var folderPath = Path.GetFullPath(Path.GetDirectoryName(RepoFilePath));
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            Environment.SetEnvironmentVariable(Startup.RepositoryFilePathEnvVar, RepoFilePath);
            var hostBuilder = new WebHostBuilder()
                    .UseStartup<Startup>();
            _server = new TestServer(hostBuilder);
        }

        [TestMethod]
        public async Task GetDirectory()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                Assert.IsNotNull(dir.Directory);
                Assert.IsNotNull(dir.NewNonce);
                Assert.IsNotNull(dir.NewAccount);
                Assert.IsNotNull(dir.NewOrder);
                Assert.IsNotNull(dir.Meta?.TermsOfService);
            }
        }

        [TestMethod]
        public async Task NewNonce()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var requ = new HttpRequestMessage(
                        HttpMethod.Head, dir.NewNonce);
                var resp = await http.SendAsync(requ);

                resp.EnsureSuccessStatusCode();

                Assert.AreEqual(HttpStatusCode.NoContent, resp.StatusCode);
                Assert.IsTrue(resp.Headers.Contains(Constants.ReplayNonceHeaderName),
                        "contains nonce response header");
                
                using (var acme = new AcmeProtocolClient(http, dir))
                {
                    Assert.IsNull(acme.NextNonce);
                    await acme.GetNonceAsync();
                    Assert.IsNotNull(acme.NextNonce);
                }
            }
        }

        [TestMethod]
        public async Task NewAccount()
        {
            using (var http = _server.CreateClient())
            {
                var dir =await GetDir();
                using (var acme = new AcmeProtocolClient(http, dir))
                {
                    Assert.IsNull(acme.NextNonce);
                    await acme.GetNonceAsync();
                    Assert.IsNotNull(acme.NextNonce);

                    await acme.CreateAccountAsync(new[] { "foo@mailinator.com" });
                }
            }
        }

        [TestMethod]
        public async Task GetAccount()
        {
            using (var http = _server.CreateClient())
            {
                var acctJson = await http.GetStringAsync("http://localhost/acme/acct/1");
            }
        }

        [TestMethod]
        public async Task NewOrder()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var dnsIds = new[] { "foo.mock.acme2.zyborg.io" };

                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                AccountDetails acct;

                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;
                }

                var badSigner = new Crypto.JOSE.Impl.RSJwsTool();
                badSigner.Init();

                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: badSigner))
                {
                    await acme.GetNonceAsync();
                    acme.Account = acct;
                    
                    await Assert.ThrowsExceptionAsync<Exception>(
                        async () => await acme.CreateOrderAsync(dnsIds));
                }

                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    acme.Account = acct;

                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);

                    var order2 = await acme.GetOrderDetailsAsync(order.OrderUrl);
                    Assert.AreEqual(order?.Payload?.Finalize, order2?.Payload?.Finalize);
                }
            }
        }

        [TestMethod]
        public async Task GetAuthz()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var dnsIds = new[] { "foo.mock.acme2.zyborg.io" };
                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Authorizations?.Length);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Identifiers?.Length);

                    var authz = await acme.GetAuthorizationDetailsAsync(
                            order.Payload.Authorizations[0]);
                    Assert.IsNotNull(authz);
                    Assert.IsFalse(authz.Wildcard ?? false);
                    Assert.AreEqual(dnsIds[0], authz.Identifier.Value);
                }
            }
        }

        [TestMethod]
        public async Task GetAuthzWildcard()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var dnsIds = new[] { "*.mock.acme2.zyborg.io" };
                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Authorizations?.Length);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Identifiers?.Length);

                    var authz = await acme.GetAuthorizationDetailsAsync(
                            order.Payload.Authorizations[0]);
                    Assert.IsNotNull(authz);
                    Assert.IsTrue(authz.Wildcard ?? false);
                    Assert.AreEqual(dnsIds[0], authz.Identifier.Value);
                }
            }
        }

        [TestMethod]
        public async Task GetAuthzMulti()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var dnsIds = new[]
                    {
                        "foo1.mock.acme2.zyborg.io",
                        "foo2.mock.acme2.zyborg.io",
                        "foo3.mock.acme2.zyborg.io",
                    };
                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Authorizations?.Length);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Identifiers?.Length);

                    var dnsIdsList = new List<string>(dnsIds);
                    foreach (var authzUrl in order.Payload.Authorizations)
                    {
                        var authz = await acme.GetAuthorizationDetailsAsync(authzUrl);
                        Assert.IsNotNull(authz);
                        Assert.IsFalse(authz.Wildcard ?? false);
                        Assert.IsTrue(dnsIdsList.Remove(authz.Identifier.Value),
                                "DNS Identifiers contains authz DNS Identifier");
                    }
                    Assert.AreEqual(0, dnsIdsList.Count);
                }
            }
        }

        [TestMethod]
        public async Task GetChallenge()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var dnsIds = new[] { "foo.mock.acme2.zyborg.io" };
                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Authorizations?.Length);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Identifiers?.Length);

                    var authzUrl = order.Payload.Authorizations[0];
                    var authz = await acme.GetAuthorizationDetailsAsync(authzUrl);
                    Assert.IsNotNull(authz);
                    Assert.IsFalse(authz.Wildcard ?? false);
                    Assert.AreEqual(dnsIds[0], authz.Identifier.Value);

                    foreach (var chlng in authz.Challenges)
                    {
                        var chlng2 = await acme.GetChallengeDetailsAsync(chlng.Url);
                        Assert.IsNotNull(chlng2);
                    }
                }
            }
        }

        [TestMethod]
        public async Task AnswerChallenge()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var dnsIds = new[] { "foo.mock.acme2.zyborg.io" };

                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Authorizations?.Length);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Identifiers?.Length);

                    var authzUrl = order.Payload.Authorizations[0];
                    var authz = await acme.GetAuthorizationDetailsAsync(authzUrl);
                    Assert.IsNotNull(authz);
                    Assert.IsFalse(authz.Wildcard ?? false);
                    Assert.AreEqual(dnsIds[0], authz.Identifier.Value);

                    foreach (var chlng in authz.Challenges)
                    {
                        var chlng2 = await acme.AnswerChallengeAsync(chlng.Url);
                        Assert.IsNotNull(chlng2);
                        Assert.AreEqual("valid", chlng2.Status);
                    }
                }
            }
        }


        [TestMethod]
        public async Task FinalizeOrder()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var dnsIds = new[] {
                        "foo.mock.acme2.zyborg.io",
                        "foo-alt-1.mock.acme2.zyborg.io",
                        "foo-alt-2.mock.acme2.zyborg.io",
                        "foo-alt-3.mock.acme2.zyborg.io",
                    };
                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Authorizations?.Length);
                    Assert.AreEqual(dnsIds.Length, order.Payload.Identifiers?.Length);

                    var authzUrl = order.Payload.Authorizations[0];
                    var authz = await acme.GetAuthorizationDetailsAsync(authzUrl);
                    Assert.IsNotNull(authz);
                    Assert.IsFalse(authz.Wildcard ?? false);
                    Assert.AreEqual(dnsIds[0], authz.Identifier.Value);

                    foreach (var chlng in authz.Challenges)
                    {
                        var chlng2 = await acme.AnswerChallengeAsync(chlng.Url);
                        Assert.IsNotNull(chlng2);
                        Assert.AreEqual("valid", chlng2.Status);
                    }

                    var kpr = PkiKeyPair.GenerateRsaKeyPair(2048);
                    var csr = new PkiCertificateSigningRequest($"cn={dnsIds[0]}", kpr,
                            PkiHashAlgorithm.Sha256);
                    csr.CertificateExtensions.Add(
                            PkiCertificateExtension.CreateDnsSubjectAlternativeNames(dnsIds.Skip(1)));
                    var csrDer = csr.ExportSigningRequest(PkiEncodingFormat.Der);
                    
                    var finalizedOrder = await acme.FinalizeOrderAsync(order.Payload.Finalize, csrDer);
                    Assert.AreEqual("valid", finalizedOrder.Payload.Status);
                    Assert.IsNotNull(finalizedOrder.Payload.Certificate);

                    var getResp = await acme.GetAsync(finalizedOrder.Payload.Certificate);
                    getResp.EnsureSuccessStatusCode();

                    using (var fs = new FileStream(
                            @"C:\local\prj\bek\ACMESharp\ACMESharpCore\test\ACMESharp.MockServer.UnitTests\finalize-cert.pem",
                            FileMode.Create))
                    {
                        await getResp.Content.CopyToAsync(fs);
                    }
                }
            }
        }


        [TestMethod]
        public async Task RevokeCertificate()
        {
            using (var http = _server.CreateClient())
            {
                var dir = await GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    // TODO go through the motions as the FinalizeOrder test?
                    await acme.RevokeCertificateAsync(new byte[] { });
                }
            }
        }


        private async Task<ServiceDirectory> GetDir()
        {
            // var dir = new ServiceDirectory
            // {
            //     Directory = $"{DefaultServerUrl}acme/directory",
            //     NewNonce = $"{DefaultServerUrl}acme/new-nonce",
            //     NewAccount = $"{DefaultServerUrl}acme/new-acct",
            //     NewOrder = $"{DefaultServerUrl}acme/new-order",
            // };
            // return Task.FromResult(dir);

            using (var http = _server.CreateClient())
            {
                using (var acme = new AcmeProtocolClient(http))
                {
                    var dir = await acme.GetDirectoryAsync();
                    return dir;
                }
            }
        }
    }
}
