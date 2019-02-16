using System;
using ACMESharp.Protocol.Resources;
using Microsoft.AspNetCore.Mvc;

namespace ACMESharp.MockServer.Controllers
{
    [Route(AcmeDirectoryController.ControllerRoute)]
    [ApiController]
    public class AcmeDirectoryController : ControllerBase
    {
        public const string ControllerRoute = "directory";

        [HttpGet("")]
        public ActionResult<object> GetDirectory()
        {
            var dir = new ServiceDirectory
            {
                Directory = Url.Action(nameof(GetDirectory),
                        controller: null,
                        values: null, protocol: Request.Scheme),
                NewNonce = Url.Action(nameof(AcmeController.NewNonce),
                        controller: AcmeController.ControllerRoute,
                        values: null, protocol: Request.Scheme),
                NewAccount = Url.Action(nameof(AcmeController.NewAccount),
                        controller: AcmeController.ControllerRoute,
                        values: null, protocol: Request.Scheme),
                NewOrder = Url.Action(nameof(AcmeController.NewOrder),
                        controller: AcmeController.ControllerRoute,
                        values: null, protocol: Request.Scheme),
                NewAuthz = null,
                KeyChange = null,
                RevokeCert = Url.Action(nameof(AcmeController.Revoke),
                        controller: AcmeController.ControllerRoute,
                        values: null, protocol: Request.Scheme),

                Meta = new DirectoryMeta
                {
                    TermsOfService = Url.Action(nameof(GetTermsOfService),
                            controller: null, values: null, protocol: Request.Scheme),
                    Website = null,
                    CaaIdentities = null,
                    ExternalAccountRequired = null,
                }
            };

            var random = Guid.NewGuid().ToString().Replace("-", "");
          //dir.SetExtra($"prop_{random}", random);
            dir.SetExtra($"prop_{random}",
                    "https://community.letsencrypt.org/t/adding-random-entries-to-the-directory/");

            return dir;
        }

        [HttpGet("tos")]
        public ActionResult<string> GetTermsOfService()
        {
            return "No Terms!";
        }
    }
}