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

namespace ACMESharp.MockServer.UnitTests
{
    [TestClass]
    public class AcmeControllerTests
    {
        // When using the ASP.NET Core TestHost, only the URL Path is significant
        public const string DefaultServerUrl = "http://localhost/";
        public const string RepoFilePath = @"C:\local\prj\bek\ACMESharp\ACMESharpCore\test\ACMESharp.MockServer.UnitTests\acme-mockserver.db";

        static TestServer _server;

        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            if (File.Exists(RepoFilePath))
                File.Delete(RepoFilePath);

            Environment.SetEnvironmentVariable(Startup.RepositoryFilePathEnvVar, RepoFilePath);
            var hostBuilder = new WebHostBuilder()
                    .UseStartup<Startup>();
            _server = new TestServer(hostBuilder);
        }

        [TestMethod]
        public async Task NewNonce()
        {
            using (var http = _server.CreateClient())
            {
                var dir = GetDir();
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
                var dir = GetDir();
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
                var dir = GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var order = await acme.CreateOrderAsync(new[] { "foo.zyborg.io" });
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
                var dir = GetDir();
                var signer = new Crypto.JOSE.Impl.RSJwsTool();
                signer.Init();
                using (var acme = new AcmeProtocolClient(http, dir,
                    signer: signer))
                {
                    await acme.GetNonceAsync();
                    var acct = await acme.CreateAccountAsync(new[] { "mailto:foo@bar.com" });
                    acme.Account = acct;

                    var dnsIds = new[] { "foo.zyborg.io" };
                    var order = await acme.CreateOrderAsync(dnsIds);
                    Assert.IsNotNull(order?.OrderUrl);
                    Assert.AreEqual(1, order.Payload.Authorizations?.Length);
                    Assert.AreEqual(1, order.Payload.Identifiers?.Length);

                    var authz = await acme.GetAuthorizationDetailsAsync(
                            order.Payload.Authorizations[0]);
                    Assert.IsNotNull(authz);
                    Assert.AreEqual(dnsIds[0], authz.Identifier.Value);
                }
            }
        }

        private ServiceDirectory GetDir()
        {
            var dir = new ServiceDirectory
            {
                Directory = $"{DefaultServerUrl}acme/directory",
                NewNonce = $"{DefaultServerUrl}acme/new-nonce",
                NewAccount = $"{DefaultServerUrl}acme/new-acct",
                NewOrder = $"{DefaultServerUrl}acme/new-order",
            }.Directory;
            return dir;
        }
    }
}
