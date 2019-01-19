using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ACMEKestrel.Crypto;
using ACMESharp.Authorizations;
using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Examples.Common.PKI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ACMEKestrel
{
    public class AcmeHostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;
        private readonly AcmeOptions _options;
        private readonly AcmeState _state;
        private Timer _timer;

        public AcmeHostedService(ILogger<AcmeHostedService> logger,
            IServiceProvider services,
            AcmeOptions options, AcmeState state)
        {
            _logger = logger;
            _services = services;
            _options = options;
            _state = state;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ACME Hosted Service is staring");

            _state.RootDir = Path.Combine(Directory.GetCurrentDirectory(),
                    _options.AcmeRootDir ?? ".");
            _state.ServiceDirectoryFile = Path.Combine(_state.RootDir, "00-ServiceDirectory.json");
            _state.TermsOfServiceFile = Path.Combine(_state.RootDir, "05-TermsOfService");
            _state.AccountFile = Path.Combine(_state.RootDir, "10-Account.json");
            _state.AccountKeyFile = Path.Combine(_state.RootDir, "15-AccountKey.json");
            _state.OrderFile = Path.Combine(_state.RootDir, "50-Order.json");
            _state.AuthorizationsFile = Path.Combine(_state.RootDir, "52-Authorizations.json");
            _state.CertificateKeysFile = Path.Combine(_state.RootDir, "70-CertificateKeys.pem");
            _state.CertificateRequestFile = Path.Combine(_state.RootDir, "72-CertificateRequest.der");
            _state.CertificateChainFile = Path.Combine(_state.RootDir, "74-CertificateChain.pem");
            _state.CertificateFile = Path.Combine(_state.RootDir, "80-Certificate.pfx");

            if (!Directory.Exists(_state.RootDir))
                Directory.CreateDirectory(_state.RootDir);

            (_, _state.ServiceDirectory) = Load<ServiceDirectory>(_state.ServiceDirectoryFile);
            (_, _state.Account) = Load<AccountDetails>(_state.AccountFile);
            (_, _state.AccountKey) = Load<AccountKey>(_state.AccountKeyFile);
            (_, _state.Order) = Load<OrderDetails>(_state.OrderFile);
            (_, _state.Authorizations) = Load<Dictionary<string, Authorization>>(
                    _state.AuthorizationsFile);
            (_, var certRaw) = Load<byte[]>(_state.CertificateFile);
            if (certRaw?.Length > 0)
                _state.Certificate = new X509Certificate2(certRaw);

            _logger.LogInformation("Preparing to launch background task...");
            // We delay for 5 seconds just to give other parts of
            // the service (like request handling) to get in place
            Task.Delay(5 * 1000);
            _timer = new Timer(DoTheWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(300));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ACME Hosted Service is stopping");
            _timer?.Change(Timeout.Infinite, 0);

            _state.Certificate?.Dispose();
            
            return Task.CompletedTask;
        }

        protected void DoTheWork(object state)
        {
            var task = DoTheWorkAsync(state);
            task.Wait();
        }

        protected async Task DoTheWorkAsync(object state)
        {
            _logger.LogInformation("** DOING WORKING *****************************************");
            _logger.LogInformation($"DNS Names:  {string.Join(",", _options.DnsNames)}");

            if (_state.Certificate != null)
            {
                var now = DateTime.Now;
                if (_state.Certificate.NotBefore > now && _state.Certificate.NotAfter < now)
                {
                    _logger.LogInformation("Existing certificate is Good!");
                    return;
                }
                {
                    _logger.LogWarning("Existing Certificate is Expired!");
                }
            }
            else
            {
                _logger.LogWarning("Missing Certificate");
            }

            var acmeUrl = new Uri(_options.CaUrl);
            using (var acme = new AcmeProtocolClient(acmeUrl))
            {
                _state.ServiceDirectory = await acme.GetDirectoryAsync();
                Save(_state.ServiceDirectoryFile, _state.ServiceDirectory);
                acme.Directory = _state.ServiceDirectory;

                Save(_state.TermsOfServiceFile,
                        await acme.GetTermsOfServiceAsync());

                await acme.GetNonceAsync();

                if (!await ResolveAccount(acme))
                    return;

                if (!await ResolveOrder(acme))
                    return;

                if (!await ResolveChallenges(acme))
                    return;
                
                if (!await ResolveAuthorizations(acme))
                    return;
                
                if (!await ResolveCertificate(acme))
                    return;
            }
        }

        protected async Task<bool> ResolveAccount(AcmeProtocolClient acme)
        {
            // TODO:  All this ASSUMES a fixed key type/size for now
            if (_state.Account == null || _state.AccountKey == null)
            {
                var contacts = _options.AccountContactEmails.Select(x => $"mailto:{x}");
                _logger.LogInformation("Creating ACME Account");
                _state.Account = await acme.CreateAccountAsync(
                        contacts: contacts,
                        termsOfServiceAgreed: _options.AcceptTermsOfService);
                _state.AccountKey = new AccountKey
                {
                    KeyType = acme.Signer.JwsAlg,
                    KeyExport = acme.Signer.Export(),
                };
                Save(_state.AccountFile, _state.Account);
                Save(_state.AccountKeyFile, _state.AccountKey);
                acme.Account = _state.Account;
            }
            else
            {
                acme.Account = _state.Account;
                acme.Signer.Import(_state.AccountKey.KeyExport);
            }

            return true;
        }

        protected async Task<bool> ResolveOrder(AcmeProtocolClient acme)
        {
            var now = DateTime.Now;
            if (!string.IsNullOrEmpty(_state.Order?.OrderUrl))
            {
                _logger.LogInformation("Existing Order found; refreshing");
                _state.Order = await acme.GetOrderDetailsAsync(
                        _state.Order.OrderUrl, _state.Order);
            }

            if (_state.Order?.Payload?.Error != null)
            {
                _logger.LogWarning("Existing Order reported an Error:");
                _logger.LogWarning(JsonConvert.SerializeObject(_state.Order.Payload.Error));
                _logger.LogWarning("Resetting existing order");
                _state.Order = null;
            }

            if (AcmeState.InvalidStatus == _state.Order?.Payload.Status)
            {
                _logger.LogInformation("Existing Order is INVALID; resetting");
                _state.Order = null;
            }

            if (!DateTime.TryParse(_state.Order?.Payload?.Expires, out var orderExpires)
                || orderExpires < now)
            {
                _logger.LogInformation("Existing Order is EXPIRED; resetting");
                _state.Order = null;
            }

            if (DateTime.TryParse(_state.Order?.Payload?.NotAfter, out var orderNotAfter)
                && orderNotAfter < now)
            {
                _logger.LogInformation("Existing Order is OUT-OF-DATE; resetting");
                _state.Order = null;
            }

            if (_state.Order?.Payload == null)
            {
                _logger.LogInformation("Creating NEW Order");
                _state.Order = await acme.CreateOrderAsync(_options.DnsNames);
            }

            Save(_state.OrderFile, _state.Order);
            return true;
        }

        protected async Task<bool> ResolveChallenges(AcmeProtocolClient acme)
        {
            if (AcmeState.PendingStatus == _state.Order?.Payload?.Status)
            {
                _logger.LogInformation("Order is pending, resolving Authorizations");
                if (_state.Authorizations == null)
                    _state.Authorizations = new Dictionary<string, Authorization>();
                foreach (var authzUrl in _state.Order.Payload.Authorizations)
                {
                    var authz = await acme.GetAuthorizationDetailsAsync(authzUrl);
                    _state.Authorizations[authzUrl] = authz;

                    if (AcmeState.PendingStatus == authz.Status)
                    {
                        foreach (var chlng in authz.Challenges)
                        {
                            if (string.IsNullOrEmpty(_options.ChallengeType)
                                || _options.ChallengeType == chlng.Type)
                            {
                                var chlngValidation = AuthorizationDecoder.DecodeChallengeValidation(
                                        authz, chlng.Type, acme.Signer);
                                if (_options.ChallengeHandler(_services, chlngValidation))
                                {
                                    _logger.LogInformation("Challenge Handler has handled challenge:");
                                    _logger.LogInformation(JsonConvert.SerializeObject(chlngValidation, Formatting.Indented));
                                    var chlngUpdated = await acme.AnswerChallengeAsync(chlng.Url);
                                    if (chlngUpdated.Error != null)
                                    {
                                        _logger.LogError("Submitting Challenge Answer reported an error:");
                                        _logger.LogError(JsonConvert.SerializeObject(chlngUpdated.Error));
                                    }
                                }

                                _logger.LogInformation("Refreshing Authorization status");
                                authz = await acme.GetAuthorizationDetailsAsync(authzUrl);
                                if (AcmeState.PendingStatus != authz.Status)
                                    break;
                            }
                        }
                    }
                }
                Save(_state.AuthorizationsFile, _state.Authorizations);

                _logger.LogInformation("Refreshing Order status");
                _state.Order = await acme.GetOrderDetailsAsync(_state.Order.OrderUrl, _state.Order);
                Save(_state.OrderFile, _state.Order);
            }
            return true;
        }

        protected async Task<bool> ResolveAuthorizations(AcmeProtocolClient acme)
        {
            if (AcmeState.InvalidStatus == _state.Order?.Payload?.Status)
            {
                _logger.LogError("Current Order is INVALID; aborting");
                return false;
            }

            if (AcmeState.ValidStatus == _state.Order?.Payload?.Status)
            {
                _logger.LogError("Current Order is already VALID; skipping");
                return true;
            }

            var now = DateTime.Now;
            do
            {
                // Wait for all Authorizations to be valid or any one to go invalid
                int validCount = 0;
                int invalidCount = 0;
                foreach (var authz in _state.Authorizations)
                {
                    switch (authz.Value.Status)
                    {
                        case AcmeState.ValidStatus:
                            ++validCount;
                            break;
                        case AcmeState.InvalidStatus:
                            ++invalidCount;
                            break;
                    }
                }

                if (validCount == _state.Authorizations.Count)
                {
                    _logger.LogInformation("All Authorizations ({0}) are valid", validCount);
                    break;
                }

                if (invalidCount > 0)
                {
                    _logger.LogError("Found {0} invalid Authorization(s); ABORTING", invalidCount);
                    return false;
                }

                _logger.LogWarning("Found {0} Authorization(s) NOT YET valid",
                        _state.Authorizations.Count - validCount);
                
                if (now.AddSeconds(_options.WaitForAuthorizations) < DateTime.Now)
                {
                    _logger.LogError("Timed out waiting for Authorizations; ABORTING");
                    return false;
                }

                // We wait in 5s increments
                await Task.Delay(5000);
                foreach (var authzUrl in _state.Order.Payload.Authorizations)
                {
                    // Update all the Authorizations still pending
                    if (AcmeState.PendingStatus == _state.Authorizations[authzUrl].Status)
                        _state.Authorizations[authzUrl] =
                                await acme.GetAuthorizationDetailsAsync(authzUrl);
                }
            } while (true);

            return true;
        }

        protected async Task<bool> ResolveCertificate(AcmeProtocolClient acme)
        {
            if (_state.Certificate != null)
            {
                _logger.LogInformation("Certificate is already resolved");
                return true;
            }

            CertPrivateKey key = null;
            _logger.LogInformation("Refreshing Order status");
            _state.Order = await acme.GetOrderDetailsAsync(_state.Order.OrderUrl, _state.Order);
            Save(_state.OrderFile, _state.Order);

            if (AcmeState.PendingStatus == _state.Order.Payload.Status)
            {
                _logger.LogInformation("Generating CSR");
                byte[] csr;
                switch (_options.CertificateKeyAlgor)
                {
                    case "rsa":
                        key = CertHelper.GenerateRsaPrivateKey(
                                _options.CertificateKeySize ?? AcmeOptions.DefaultRsaKeySize);
                        csr = CertHelper.GenerateRsaCsr(_options.DnsNames, key);
                        break;
                    case "ec":
                        key = CertHelper.GenerateEcPrivateKey(
                                _options.CertificateKeySize ?? AcmeOptions.DefaultEcKeySize);
                        csr = CertHelper.GenerateEcCsr(_options.DnsNames, key);
                        break;
                    default:
                        throw new Exception("Unknown Certificate Key Algorithm: "
                                + _options.CertificateKeyAlgor);
                }

                using (var keyPem = new MemoryStream())
                {
                    CertHelper.ExportPrivateKey(key, EncodingFormat.PEM, keyPem);
                    keyPem.Position = 0L;
                    Save(_state.CertificateKeysFile, keyPem);
                }
                Save(_state.CertificateRequestFile, csr);

                _logger.LogInformation("Finalizing Order");
                _state.Order = await acme.FinalizeOrderAsync(_state.Order.Payload.Finalize, csr);
                Save(_state.OrderFile, _state.Order);
            }

            if (AcmeState.ValidStatus != _state.Order.Payload.Status)
            {
                _logger.LogWarning("Order is NOT VALID");
                return false;
            }

            if (string.IsNullOrEmpty(_state.Order.Payload.Certificate))
            {
                _logger.LogWarning("Order Certificate is NOT READY YET");
                var now = DateTime.Now;
                do
                {
                    _logger.LogInformation("Waiting...");
                    // We wait in 5s increments
                    await Task.Delay(5000);

                    _state.Order = await acme.GetOrderDetailsAsync(_state.Order.OrderUrl, _state.Order);
                    Save(_state.OrderFile, _state.Order);

                    if (!string.IsNullOrEmpty(_state.Order.Payload.Certificate))
                        break;

                    if (DateTime.Now < now.AddSeconds(_options.WaitForCertificate))
                    {
                        _logger.LogWarning("Timed Out!");
                        return false;
                    }
                } while (true);
            }

            _logger.LogInformation("Retreiving Certificate");
            var certBytes = await acme.GetOrderCertificateAsync(_state.Order);
            Save(_state.CertificateChainFile, certBytes);

            if (key == null)
            {
                _logger.LogInformation("Loading private key");
                key = CertHelper.ImportPrivateKey(EncodingFormat.PEM,
                        Load<Stream>(_state.CertificateKeysFile).value);
            }

            using (var crtStream = new MemoryStream(certBytes))
            using (var pfxStream = new MemoryStream())
            {
                _logger.LogInformation("Reading in Certificate chain (PEM)");
                var cert = CertHelper.ImportCertificate(EncodingFormat.PEM, crtStream);
                _logger.LogInformation("Writing out Certificate archive (PKCS12)");
                CertHelper.ExportArchive(key, new[] { cert }, ArchiveFormat.PKCS12, pfxStream);
                pfxStream.Position = 0L;
                Save(_state.CertificateFile, pfxStream);
            }

            _logger.LogInformation("Loading PKCS12 archive as active certificate");
            _state.Certificate = new X509Certificate2(Load<byte[]>(_state.CertificateFile).value);

            return true;
       }

        protected (bool exists, T value) Load<T>(string path, T def = default(T))
        {
            if (!File.Exists(path))
                return (false, def);

            if (typeof(T) == typeof(Stream))
            {
                return (true, (T)(object)new FileStream(path, FileMode.Open));
            }

            if (typeof(T) == typeof(byte[]))
            {
                return (true, (T)(object)File.ReadAllBytes(path));
            }

            return (true, JsonConvert.DeserializeObject<T>(
                    File.ReadAllText(path)));
        }

        protected void Save<T>(string path, T value)
        {
            if (typeof(Stream).IsAssignableFrom(typeof(T)))
            {
                var rs = (Stream)(object)value;
                using (var ws = new FileStream(path, FileMode.Create))
                {
                    rs.CopyTo(ws);
                }
            }
            else if (typeof(T) == typeof(byte[]))
            {
                var ba = (byte[])(object)value;
                File.WriteAllBytes(path, ba);
            }
            else
            {
                File.WriteAllText(path,
                        JsonConvert.SerializeObject(value, Formatting.Indented));
            }
        }

        #region IDisposable Support
        public bool IsDisposed { get; private set; } // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _timer?.Dispose();
                    _timer = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                IsDisposed = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AcmeHostedService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}