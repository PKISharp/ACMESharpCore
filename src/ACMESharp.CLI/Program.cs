using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ACMESharp.CLI
{
    class Program
    {
        /// https://letsencrypt.org/docs/staging-environment/
        /// https://letsencrypt.status.io/

        public const string LetsEncryptStagingEndpoint = "https://acme-staging.api.letsencrypt.org/";
        public const string LetsEncryptV2StagingEndpoint = "https://acme-staging-v02.api.letsencrypt.org/";
        public const string LetsEncryptEndpoint = "https://acme-v01.api.letsencrypt.org/";
        public const string LetsEncryptV2Endpoint = "https://acme-v02.api.letsencrypt.org/";


        static async Task Main(string[] args)
        {
            // Need a place to stash stuff
            if (!Directory.Exists("_IGNORE"))
                Directory.CreateDirectory("_IGNORE");

            var client = new AcmeClient(new Uri(LetsEncryptV2StagingEndpoint));
            client.AfterHttpSend = AftertHttpSend;


            var dir = await client.GetDirectoryAsync();

            File.WriteAllText("_IGNORE\\000-dir.json", JsonConvert.SerializeObject(dir, Formatting.Indented));
            client.Directory = dir;

            Console.WriteLine("Nonce Before: " + client.NextNonce);
            await client.GetNonceAsync();
            Console.WriteLine("Nonce After: " + client.NextNonce);

            var acct = await client.CreateAccountAsync(new[] { "mailto:foo@bar.com" }, true);
            File.WriteAllText("_IGNORE\\010-acct.json", JsonConvert.SerializeObject(acct, Formatting.Indented));
            client.Account = acct;
        }

        static void AftertHttpSend(string methodName, HttpResponseMessage resp)
        {
            if (methodName == nameof(AcmeClient.CreateAccountAsync))
            {
                var headers = resp.Headers.Concat(resp.Content.Headers).Select(x => $"// {x.Key}: {string.Join(",", x.Value)}");
                File.WriteAllText("_IGNORE\\010-acct-HttpResponse.json",
                        string.Join("\r\n", headers));
                File.AppendAllText("_IGNORE\\010-acct-HttpResponse.json",
                        "\r\n" + resp.Content.ReadAsStringAsync().Result);
            }
        }
    }
}
