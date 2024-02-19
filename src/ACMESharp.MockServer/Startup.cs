using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PKISharp.SimplePKI;

namespace ACMESharp.MockServer
{
    public class Startup
    {
        public const string RepositoryFilePathEnvVar = "ACME_REPO_PATH";
        public const string RepositoryFilePath = "acme-mockserver.db";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var repoPath = Environment.GetEnvironmentVariable(RepositoryFilePathEnvVar)
                    ?? RepositoryFilePath;
            var repo = Storage.Impl.LiteDbRepo.GetInstance(repoPath);

            services.AddSingleton<Storage.IRepository>(repo);
            services.AddSingleton<INonceManager, RepoNonceManager>();
            services.AddSingleton(new CertificateAuthority(new CertificateAuthority.Options
            {
                CaKeyPairSavePath = "ca-kypr.save",
                KeyPairAlgorithm = PkiAsymmetricAlgorithm.Rsa,
                BitLength = 2048,
                CaCertificateSavePath = "ca-cert.save",
                CaSubjectName = "cn=mock-acme-ca",
                SignatureHashAlgorithm = PkiHashAlgorithm.Sha512,
            }));


            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }
    }
}
