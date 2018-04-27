using System;
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
            var client = new AcmeClient(new Uri(LetsEncryptV2StagingEndpoint));
            var dir = await client.GetDirectoryAsync();

            Console.WriteLine(JsonConvert.SerializeObject(dir, Formatting.Indented));
            client.Directory = dir;

            Console.WriteLine("Nonce Before: " + client.NextNonce);
            await client.GetNonceAsync();
            Console.WriteLine("Nonce After: " + client.NextNonce);

            await client.CreateAccountAsync(new[] { "mailto:foo@bar.com" }, true);
        }
    }
}
