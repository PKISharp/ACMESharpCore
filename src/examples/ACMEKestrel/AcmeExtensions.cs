using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ACMEKestrel
{
    public static class AcmeExtensions
    {
        public static IWebHostBuilder AddAcmeServices(this IWebHostBuilder builder,
            IEnumerable<string> dnsNames,
            IEnumerable<string> contactEmails = null,
            bool acceptTos = false,
            string rootDir = null)
        {
            var acmeOptions = new AcmeOptions
            {
                AccountContactEmails = contactEmails,
                AcceptTermsOfService = acceptTos,
                DnsNames = dnsNames,
            };
            if (rootDir != null)
                acmeOptions.AcmeRootDir = rootDir;
            return AddAcmeServices(builder, acmeOptions);
        }

        public static IWebHostBuilder AddAcmeServices(this IWebHostBuilder builder, AcmeOptions acmeOptions)
        {
            builder.ConfigureServices(services => {
                services.AddSingleton<AcmeOptions>(acmeOptions);
                services.AddSingleton(AcmeState.Instance);
                services.AddHostedService<AcmeHostedService>();
            });
            builder.UseKestrel((context, options) => options.ConfigureHttpsDefaults(configOpts => {
                configOpts.ServerCertificateSelector = (cc, x) => {
                    return AcmeState.Instance.Certificate;
                };
            }));
            return builder;
        }

        public static IApplicationBuilder UseAcmeChallengeHandler(this IApplicationBuilder app)
        {
            app.Map($"/{AcmeHttp01ChallengeHandler.AcmeHttp01PathPrefix}", appBuilder => {
                appBuilder.Run(AcmeHttp01ChallengeHandler.HandleHttp01ChallengeRequest);
            });

            return app;
        }
    }
}