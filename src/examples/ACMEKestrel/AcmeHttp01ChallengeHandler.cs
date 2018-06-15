using System;
using System.Net;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ACMEKestrel
{
    public class AcmeHttp01ChallengeHandler
    {
        public static readonly string AcmeHttp01PathPrefix =
                ACMESharp.Authorizations.Http01ChallengeValidationDetails.HttpPathPrefix.Trim('/');

        public static async Task HandleHttp01ChallengeRequest(HttpContext context)
        {
            var logger = context.RequestServices.GetService<ILogger<AcmeHttp01ChallengeHandler>>();
            var state = context.RequestServices.GetService<AcmeState>();
            var fullPath = $"{context.Request.PathBase}{context.Request.Path}".Trim('/');

            logger.LogInformation("Running ACME Challenge Request Handler");
            if (state.Http01Responses.TryGetValue(fullPath, out var httpDetails))
            {
                logger.LogInformation("Found match on [{0}] with response [{1}]",
                        fullPath, httpDetails.HttpResourceValue);
                context.Response.ContentType = httpDetails.HttpResourceContentType;
                await context.Response.WriteAsync(httpDetails.HttpResourceValue);
            }
            else
            {
                logger.LogInformation("NO MATCH FOUND ON [{0}]", fullPath);
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync("No matching ACME response path");
            }
        }

        public static bool AddChallengeHandling(IServiceProvider services, IChallengeValidationDetails chlngDetails)
        {
            var logger = services.GetService<ILogger<AcmeHttp01ChallengeHandler>>();
            var state = services.GetService<AcmeState>();
            var httpDetails = chlngDetails as Http01ChallengeValidationDetails;
            if (httpDetails == null)
            {
                logger.LogInformation("Unable to handle non-Http01 Challenge details");
                return false;
            }

            var fullPath = httpDetails.HttpResourcePath.Trim('/');
            logger.LogInformation("Handling Challenges with HTTP full path of [{0}]", fullPath);
            state.Http01Responses[fullPath] = httpDetails;
            return true;
        }
    }
}