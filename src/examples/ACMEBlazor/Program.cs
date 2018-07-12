using ACMESharp.Protocol;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ACMEBlazor
{
    public class Program
    {
        public const string LetsEncryptV2StagingEndpoint = "https://acme-staging-v02.api.letsencrypt.org/";

        public const string LetsEncryptV2Endpoint = "https://acme-v02.api.letsencrypt.org/";

        static void Main(string[] args)
        {
            var serviceProvider = new BrowserServiceProvider(services =>
            {
                //services.AddSingleton(new AcmeProtocolClient(new Uri(LetsEncryptV2StagingEndpoint)));
            });

            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
}
