using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACMESharp.Protocol;

namespace ACMESharp.StateManagement
{
    public class AcmeService
    {
        public const string LetsEncryptV2Endpoint = "https://acme-v02.api.letsencrypt.org/";

        public const string LetsEncryptV2StagingEndpoint = "https://acme-staging-v02.api.letsencrypt.org/";

        private AcmeProtocolClient _acme;

        private AcmeService()
        { }

        public async Task<AcmeService> Create(Uri url = null)
        {
            if (url == null)
                url = new Uri(LetsEncryptV2Endpoint);

            var svc = new AcmeService();
            await svc.Init(url);
            return svc;
        }

        protected async Task Init(Uri url)
        {
             _acme = new AcmeProtocolClient(url);
             _acme.Directory = await _acme.GetDirectoryAsync();
             await _acme.GetNonceAsync();
        }

        public async Task SaveTermsOfService(Stream stream,
            CancellationToken cancel = default)
        {
            var ret = await _acme.GetTermsOfServiceAsync(cancel);
            await stream.WriteAsync(ret.content, 0, ret.content.Length, cancel);
        }

        public async Task<AcmeAccount> CreateAccount(IEnumerable<string> emails,
            bool acceptTos, CancellationToken cancel = default)
        {
            await _acme.CreateAccountAsync(emails.Select(x => $"mailto:{x}"), acceptTos, cancel: cancel);
            return new AcmeAccount
            {
                _acme = _acme,
                _account = _acme.Account,
                _keyType = _acme.Signer.JwsAlg,
                _keyExport = _acme.Signer.Export(),
            };
        }
    }
}