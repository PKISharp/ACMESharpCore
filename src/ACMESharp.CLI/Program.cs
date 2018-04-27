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

        static string _writeResponseTag;

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

            try
            {
                await client.CheckAccountAsync();
                Console.WriteLine("DID NOT ERROR on lookup for existing account!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lookup of non-existent Account failed AS EXPECTED");
            }

            var contacts = new[] { "mailto:foo@example.com" };
            var acct = await client.CreateAccountAsync(contacts, true);
            File.WriteAllText("_IGNORE\\010-acct.json", JsonConvert.SerializeObject(acct, Formatting.Indented));
            client.Account = acct;

            _writeResponseTag = "2";
            var acct2 = await client.CreateAccountAsync(contacts);
            File.WriteAllText("_IGNORE\\011-acct2.json", JsonConvert.SerializeObject(acct, Formatting.Indented));
            _writeResponseTag = null;

            _writeResponseTag = "3";
            try
            {
                await client.CreateAccountAsync(contacts, throwOnExistingAccount: true);
                Console.Write("DID NOT ERROR on duplicate create");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Duplicate create AS EXPECTED");
            }
            _writeResponseTag = null;

            acct = await client.CheckAccountAsync();
            Console.WriteLine("Existing account lookup worked");
            File.WriteAllText("_IGNORE\\015-acct-lookup.json", JsonConvert.SerializeObject(acct, Formatting.Indented));

            acct = await client.UpdateAccountAsync(new[] { "mailto:bar@example.com", "mailto:baz@example.com" });
            File.WriteAllText("_IGNORE\\017-acct-update.json", JsonConvert.SerializeObject(acct, Formatting.Indented));
        }

        static void AftertHttpSend(string methodName, HttpResponseMessage resp)
        {
            if (methodName == nameof(AcmeClient.CreateAccountAsync))
            {
                WriteResponseToFile($"_IGNORE\\010-acct{_writeResponseTag}-HttpResponse.json");
            }
            else if (methodName == nameof(AcmeClient.CheckAccountAsync))
            {
                WriteResponseToFile($"_IGNORE\\015-acct-lookup{_writeResponseTag}-HttpResponse.json");
            }
            else if (methodName == nameof(AcmeClient.UpdateAccountAsync))
            {
                WriteResponseToFile($"_IGNORE\\017-acct-update{_writeResponseTag}-HttpResponse.json");
            }

            void WriteResponseToFile(string path)
            {
                var headers = resp.Headers.Concat(resp.Content.Headers)
                    .Select(x => $"// {x.Key}: {string.Join(",", x.Value)}");
                File.WriteAllText(path, string.Join("\r\n", headers));
                File.AppendAllText(path, "\r\n" + resp.Content.ReadAsStringAsync().Result);
            }
        }
    }
}
