using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using ACMESharp.MockServer.Storage;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Messages;
using ACMESharp.Protocol.Resources;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ACMESharp.MockServer.Controllers
{
  //[Route("api/[controller]")]
    [Route("acme")]
    [ApiController]
    public class AcmeController : ControllerBase
    {
        Repository _repo;
        INonceManager _nonceMgr;

        public AcmeController(Repository repo, INonceManager nonceMgr)
        {
            _repo = repo;
            _nonceMgr = nonceMgr;
        }

        [HttpHead("new-nonce")]
        public ActionResult NewNonce()
        {
            Response.Headers.Add(
                    Constants.ReplayNonceHeaderName,
                    Guid.NewGuid().ToString());

            return NoContent();
        }

        T ExtractPayload<T>(JwsSignedPayload signedPayload)
        {
            var payloadBytes = CryptoHelper.Base64.UrlDecode(signedPayload.Payload);
            var payloadJson = CryptoHelper.Base64.UrlDecodeToString(signedPayload.Payload);
            return JsonConvert.DeserializeObject<T>(payloadJson);
        }

        ProtectedHeader ExtractProtectedHeader(JwsSignedPayload signedPayload)
        {
            var protectedJson = CryptoHelper.Base64.UrlDecodeToString(signedPayload.Protected);
            return JsonConvert.DeserializeObject<ProtectedHeader>(protectedJson);
        }

        Uri ComputeRelativeUrl(string relPath)
        {
            var requPort = Request.Host.Port.HasValue
                ? Request.Host.Port.Value
                : Request.IsHttps ? 443 : 80;
            var requUrl = new UriBuilder(Request.Scheme, Request.Host.Host, requPort,
                    $"/acme/{relPath}").Uri;
            return requUrl;
        }

        [HttpPost("new-acct")]
        public ActionResult<Account> NewAccount([FromBody]JwsSignedPayload signedPayload)
        {
            var requ = ExtractPayload<CreateAccountRequest>(signedPayload);
            var protectedHeader = ExtractProtectedHeader(signedPayload);
            var jwk = JsonConvert.SerializeObject(protectedHeader.Jwk);

            // We start by saving an empty acct in order to compute the next ID
            var acct = new DbAccount();
            _repo.SaveAccount(acct);

            // Then compute the acct-specific URL based on the assigned ID
            // Sample Kid: https://acme-staging-v02.api.letsencrypt.org/acme/acct/6484231
            var acctId = acct.Id.ToString();
            var kid = ComputeRelativeUrl($"acct/{acctId}").ToString();

            // Then we actually fill out the details            
            acct.Details = new AccountDetails
            {
                Kid = kid,
                Payload = new Account
                {
                    Id = acctId,
                    Key = jwk,
                    Contact = requ.Contact?.ToArray(),
                    Status = "testing",
                    TermsOfServiceAgreed = true,
                    InitialIp = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = DateTime.Now.ToString(),
                }
            };
            _repo.SaveAccount(acct);

            Response.Headers.Add(
                    Constants.ReplayNonceHeaderName,
                    Guid.NewGuid().ToString());
            Response.Headers.Add(
                    "Location",
                    acct.Details.Kid);

            return acct.Details.Payload;
        }

        // NOTE THIS IS FOR DEBUG ONLY, ACME SPEC DISALLOWS THIS!
        [HttpGet("acct/{acctId}")]
        public ActionResult<Account> GetAccount(string acctId)
        {
            if (!int.TryParse(acctId, out var id))
                return NotFound();

            var acct = _repo.GetAccount(id);
            if (acct == null)
                return NotFound();

            return acct.Details.Payload;
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
