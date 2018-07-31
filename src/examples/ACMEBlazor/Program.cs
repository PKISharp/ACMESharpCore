using ACMEBlazor.Services;
using ACMEBlazor.Storage;
using ACMESharp.Protocol;
using Blazor.Extensions;
using Blazor.Extensions.Storage;
using BlazorDB;
using Microsoft.AspNetCore.Blazor.Browser.Rendering;
using Microsoft.AspNetCore.Blazor.Browser.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace ACMEBlazor
{
    public class Program
    {
        public const string LetsEncryptV2StagingEndpoint = "https://acme-staging-v02.api.letsencrypt.org/";

        public const string LetsEncryptV2Endpoint = "https://acme-v02.api.letsencrypt.org/";

        static void Main(string[] args)
        {
            var localStorage = new LocalStorage();
            Console.WriteLine("Loading App State");
            var app = new AppState();
            Console.WriteLine("  * loading Account");
            app.Account = localStorage.GetItem<BlazorAccount>(AppState.AccountKey);
            if (app.Account != null)
                Console.WriteLine("    * got Account: " + app.Account.Details.Kid);

            Console.WriteLine("  * loading Orders");
            var orderId = 1;
            var orders = new List<BlazorOrder>();
            var order = localStorage.GetItem<BlazorOrder>(AppState.OrderKey + orderId);
            while (order != null)
            {
                orders.Add(order);
                ++orderId;
                order = localStorage.GetItem<BlazorOrder>(AppState.OrderKey + orderId);
            }
            app.Orders = orders.ToArray();
            Console.WriteLine("    * got Orders: " + app.Orders.Length);

            var serviceProvider = new BrowserServiceProvider(services =>
            {
                //services.AddSingleton(new AcmeProtocolClient(new Uri(LetsEncryptV2StagingEndpoint)));

                services.AddSingleton<IRepository>(new Repository());

                services.AddBlazorDB(options =>
                {
                    options.LogDebug = true;
                    options.Assembly = typeof(Program).Assembly;
                });

                services.AddStorage();
                services.AddSingleton(app);
            });

            new BrowserRenderer(serviceProvider).AddComponent<App>("app");
        }
    }
}
