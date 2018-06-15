using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ACMEKestrel.Crypto;
using ACMESharp.Crypto;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ACMEKestrel
{
    public class Program
    {
        static readonly IEnumerable<string> DefaultDnsNames = new[] {
            "test1.example.com",
            "test-alt1.example.com",
            "test-alt21.example.com"
        };

        static IEnumerable<string> DnsNames { get; set; }

        public static void Main(string[] args)
        {
            var dnsNamesFile = ".\\_IGNORE\\dnsnames.json";
            if (File.Exists(dnsNamesFile))
                DnsNames = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(
                        File.ReadAllText(dnsNamesFile));

            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var builder = WebHost.CreateDefaultBuilder(args);
            
            // Simple Form:
            /*
            builder.AddAcmeServices(
                    Program.DnsNames ?? DefaultDnsNames,
                    contactEmails: new[] { "acmetest@mailinator.com" },
                    acceptTos: true)
            */
                
            // Full Form with access to All Options:
            builder.AddAcmeServices(new AcmeOptions
                    {
                        AcmeRootDir = "_IGNORE/_acmesharp",
                        DnsNames = Program.DnsNames ?? DefaultDnsNames,
                        AccountContactEmails = new[] { "acmetest@mailinator.com" },
                        AcceptTermsOfService = true,
                        CertificateKeyAlgor = "rsa",
                    });

            builder.UseStartup<Startup>();

            return builder;
        }
    }
}

