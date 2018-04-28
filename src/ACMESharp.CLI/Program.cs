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


        static string _seq;

        static async Task Main(string[] args)
        {
            // Need a place to stash stuff
            if (!Directory.Exists("_IGNORE"))
                Directory.CreateDirectory("_IGNORE");

            var client = new AcmeClient(new Uri(LetsEncryptV2StagingEndpoint));
            client.BeforeHttpSend = BeforeHttpSend;
            client.AfterHttpSend = AftertHttpSend;


            _seq = "000";
            var dir = await client.GetDirectoryAsync();
            WriteTo("dir.json", JsonConvert.SerializeObject(dir, Formatting.Indented));
            client.Directory = dir;

            await client.GetNonceAsync();

            _seq = "005";
            try
            {
                await client.CheckAccountAsync();
                Console.WriteLine("DID NOT ERROR on lookup for existing account!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lookup of non-existent Account failed AS EXPECTED");
            }

            _seq = "010";
            var contacts = new[] { "mailto:foo@example.com" };
            var acct = await client.CreateAccountAsync(contacts, true);
            WriteTo("acct.json", JsonConvert.SerializeObject(acct, Formatting.Indented));
            client.Account = acct;

            _seq = "011";
            var acct2 = await client.CreateAccountAsync(contacts);
            WriteTo("acct.json", JsonConvert.SerializeObject(acct, Formatting.Indented));

            _seq = "012";
            try
            {
                await client.CreateAccountAsync(contacts, throwOnExistingAccount: true);
                Console.Write("DID NOT ERROR on duplicate create");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine("Duplicate create AS EXPECTED");
            }

            _seq = "015";
            acct = await client.CheckAccountAsync();
            Console.WriteLine("Existing account lookup worked");
            WriteTo("acct-lookup.json", JsonConvert.SerializeObject(acct, Formatting.Indented));

            _seq = "017";
            acct = await client.UpdateAccountAsync(new[] { "mailto:bar@example.com", "mailto:baz@example.com" });
            WriteTo("acct-update.json", JsonConvert.SerializeObject(acct, Formatting.Indented));

            _seq = "200";
            var newKey = new Crypto.JOSE.Impl.RSJwsTool();
            newKey.Init();
            await client.ChangeAccountKeyAsync(newKey);

            _seq = "250";
            acct = await client.UpdateAccountAsync(new[] { "mailto:foo@example.com", "mailto:bar@example.com", "mailto:baz@example.com" });
            WriteTo("acct-update.json", JsonConvert.SerializeObject(acct, Formatting.Indented));

            _seq = "290";
            await client.DeactivateAccountAsync();
            WriteTo("acct-deactivate.json", JsonConvert.SerializeObject(acct, Formatting.Indented));
            try
            {
                acct = await client.UpdateAccountAsync(new[] { "mailto:foo@example.com", "mailto:bar@example.com", "mailto:baz@example.com" });
                Console.Write("DID NOT ERROR on deactivated Account update");
                WriteTo("acct-update.json", JsonConvert.SerializeObject(acct, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Console.Write("Failed to update deactivated Account AS EXPECTED");
            }
        }

        static void WriteTo(string name, string value)
        {
            File.WriteAllText($"_IGNORE\\{_seq}-{name}", value);
        }

        static void AppendTo(string name, string value)
        {
            File.AppendAllText($"_IGNORE\\{_seq}-{name}", value);
        }

        static void BeforeHttpSend(string methodName, HttpRequestMessage requ)
        {
            var toName = $"{methodName}-HttpRequest.json";
            var headers = (requ.Content == null ? requ.Headers : requ.Headers.Concat(requ.Content.Headers))
                .Select(x => $"// {x.Key}: {string.Join(",", x.Value)}");
            WriteTo(toName, string.Join("\r\n", headers));
            if (requ.Content != null)
                AppendTo(toName, "\r\n" + requ.Content.ReadAsStringAsync().Result);
        }
        static void AftertHttpSend(string methodName, HttpResponseMessage resp)
        {
            var toName = $"{methodName}-HttpResponse.json";
            WriteTo(toName, string.Join("\r\n", "// " + resp.StatusCode + "\r\n"));
            var headers = resp.Headers.Concat(resp.Content.Headers)
                .Select(x => $"// {x.Key}: {string.Join(",", x.Value)}");
            AppendTo(toName, string.Join("\r\n", headers));
            AppendTo(toName, "\r\n" + resp.Content.ReadAsStringAsync().Result);
        }
    }
}
