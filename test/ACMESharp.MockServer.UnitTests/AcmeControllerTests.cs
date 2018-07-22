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

namespace ACMESharp.MockServer.UnitTests
{
    [TestClass]
    public class AcmeControllerTests
    {
        // When using the ASP.NET Core TestHost, only the URL Path is significant
        public const string DefaultServerUrl = "http://localhost/";

        static TestServer _server;

        [ClassInitialize]
        public static void Init(TestContext ctx)
        {
            var hostBuilder = new WebHostBuilder()
                    .UseStartup<Startup>();
            _server = new TestServer(hostBuilder);
        }

        [TestMethod]
        public async Task NewNonce()
        {
            using (var http = _server.CreateClient())
            {
                var requ = new HttpRequestMessage(
                        HttpMethod.Head,
                        $"{DefaultServerUrl}acme/new-nonce");
                var resp = await http.SendAsync(requ);

                resp.EnsureSuccessStatusCode();

                Assert.AreEqual(HttpStatusCode.NoContent, resp.StatusCode);
                Assert.IsTrue(resp.Headers.Contains(Constants.ReplayNonceHeaderName),
                        "contains nonce response header");
                
                var dir = new ServiceDirectory
                {
                    NewNonce = $"{DefaultServerUrl}acme/new-nonce",
                };
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
                // var requ = new HttpRequestMessage(
                //         HttpMethod.Head,
                //         $"{DefaultServerUrl}acme/new-nonce");
                // var resp = await http.SendAsync(requ);

                // resp.EnsureSuccessStatusCode();

                // Assert.AreEqual(HttpStatusCode.NoContent, resp.StatusCode);
                // Assert.IsTrue(resp.Headers.Contains(Constants.ReplayNonceHeaderName),
                //         "contains nonce response header");
                
                var dir = new ServiceDirectory
                {
                    NewNonce = $"{DefaultServerUrl}acme/new-nonce",
                    NewAccount = $"{DefaultServerUrl}acme/new-acct",
                };
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
    }
}
