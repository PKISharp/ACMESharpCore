using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ACMESharp.Authorizations;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol;
using ACMESharp.Protocol.Resources;
using Examples.Common;
using Examples.Common.PKI;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json;
using PKISharp.SimplePKI;

namespace ACMECLI
{
    [Command]
    class Program
    {
        [Option(ShortName = "", Description = "Directory to store stateful information; defaults to current")]
        public string State { get; } = ".";

        [Option(ShortName = "", Description = "Name of a predefined ACME CA base endpoint (specify invalid value to see list)")]
        [AllowedValues(
            Constants.LetsEncryptName,
            Constants.LetsEncryptStagingName,
            IgnoreCase = true
        )]
        public string CaName { get; } = Constants.LetsEncryptName;

        [Option(ShortName = "", Description = "Full URL of an ACME CA endpoint; this option overrides " + nameof(CaName))]
        public string CaUrl { get; }

        [Option(ShortName = "", Description = "Flag indicates to refresh the current cached ACME Directory of service endpoints for the target CA")]
        public bool RefreshDir { get; }

        [Option(CommandOptionType.MultipleValue,
                ShortName = "", Description = "One or more emails to be registered as account contact info (can be repeated)")]
        public IEnumerable<string> Email { get; }

        [Option(ShortName = "", Description = "Flag indicates that you agree to CA's terms of service")]
        public bool AcceptTos { get; }

        [Option(CommandOptionType.MultipleValue,
                ShortName = "", Description = "One or more DNS names to include in the cert; the first is primary subject name, subsequent are subject alternative names (can be repeated)")]
        public IEnumerable<string> Dns { get; }

        [Option(CommandOptionType.MultipleValue,
                ShortName = "", Description = "One or more DNS name servers to be used to resolve host entries, such as during testing (can be repeated)")]
        public IEnumerable<string> NameServer { get; }

        [Option(ShortName = "", Description = "Flag indicates to refresh the state of pending ACME Order")]
        public bool RefreshOrder { get; }

        [Option(ShortName = "", Description = "Indicates that only one specific Challenge type should be handled")]
        [AllowedValues(
            Constants.Dns01ChallengeType,
            Constants.Http01ChallengeType,
            IgnoreCase = true
        )]
        public string ChallengeType { get; }

        [Option(ShortName = "", Description = "Flag indicates to refresh the state of the Challenges of the pending ACME Order")]
        public bool RefreshChallenges { get; }

        [Option(ShortName = "", Description = "Flag indicates to check if the Challenges have been handled correctly")]
        public bool TestChallenges { get; }

        [Option(ShortName = "", Description = "Flag indicates to wait until Challenge tests are successfully validated, optionally override the default timeout of 300 (seconds)")]
        public (bool enabled, int? timeout) WaitForTest { get; }

        [Option(ShortName = "", Description = "Flag indicates to submit Answers to pending Challenges")]
        public bool AnswerChallenges { get; }

        [Option(ShortName = "", Description = "Flag indicates to wait until Authorizations become Valid, optionally override the default timeout of 300 (seconds)")]
        public (bool enabled, int? timeout) WaitForAuthz { get; }

        [Option(ShortName = "", Description = "Flag indicates to finalize the pending ACME Order")]
        public bool Finalize { get; }

        [Option(ShortName = "", Description = "Indicates the encryption algorithm of certificate keys, defaults to RSA")]
        [AllowedValues(
            Constants.RsaKeyType,
            Constants.EcKeyType,
            IgnoreCase = true
        )]
        public string KeyAlgor { get; } = Constants.RsaKeyType;

        [Option(ShortName = "", Description = "Indicates the encryption algorithm key size, defaults to 2048 (RSA) or 256 (EC)")]
        public int? KeySize { get; }

        [Option(ShortName = "", Description = "Flag indicates to regenerate a certificate key pair and CSR")]
        public bool RegenerateCsr { get; }

        [Option(ShortName = "", Description = "Flag indicates to refresh the local cache of an issued certificate")]
        public bool RefreshCert { get; }

        [Option(ShortName = "", Description = "Flag indicates to wait until Certificate is available, optionally override the default timeout of 300 (seconds)")]
        public (bool enabled, int? timeout) WaitForCert { get; }

        // Not enough support in .NET Core/Standard out of the box for this yet
        // [Option(ShortName = "", Description = "Save the certificate private key (PEM) to the named file path (currently only supports RSA)")]
        // public string ExportKey { get; }

        [Option(ShortName = "", Description = "Save the certificate chain (PEM) to the named file path")]
        public string ExportCert { get; }

        [Option(ShortName = "", Description = "Save the certificate chain as PFX (PKCS12) to the named file path")]
        public string ExportPfx { get; }

        [Option(ShortName = "", Description = "Includes the private key to the PFX (PKCS12) and secures with specified password (use ' ' for no password)")]
        public string ExportPfxPassword { get; }


        private string _statePath;
        private HttpClient _http;
        private AcmeProtocolClient _acme;
        private DateTime? _testWaitUntil;


        static async Task Main(string[] args) =>
                await CommandLineApplication.ExecuteAsync<Program>(args);

        public async Task OnExecute()
        {
            _statePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), State));
            Console.WriteLine("################################################################################");
            Console.WriteLine("## ACCOUNT");
            Console.WriteLine("################################################################################");
            Console.WriteLine();

            if (!Directory.Exists(_statePath))
            {
                Console.WriteLine($"Creating State Persistence Path [{_statePath}]");
                Directory.CreateDirectory(_statePath);
                Console.WriteLine();
            }

            var url = CaUrl;
            if (string.IsNullOrEmpty(url))
                Constants.NameUrlMap.TryGetValue(CaName, out url);
            if (string.IsNullOrEmpty(url))
                throw new Exception("Unresolved ACME CA URL; either name or URL argument must be specified");

            ServiceDirectory acmeDir = default;
            if (LoadStateInto(ref acmeDir, failThrow: false,
                    Constants.AcmeDirectoryFile))
            {
                Console.WriteLine("Loaded existing Service Directory");
                Console.WriteLine();
            }

            if (NameServer != null)
            {
                DnsUtil.DnsServers = NameServer.ToArray();
            }

            AccountDetails account = default;
            if (LoadStateInto(ref account, failThrow: false,
                    Constants.AcmeAccountDetailsFile))
            {
                Console.WriteLine($"Loaded Account Details for KID:");
                Console.WriteLine($"  Account KID....: {account.Kid}");
                Console.WriteLine();
            }

            IJwsTool accountSigner = default;
            ExamplesAccountKey accountKey = default;
            string accountKeyHash = default;
            if (LoadStateInto(ref accountKey, failThrow: false,
                    Constants.AcmeAccountKeyFile))
            {
                accountSigner = accountKey.GenerateTool();
                accountKeyHash = ComputeHash(accountSigner.Export());
                Console.WriteLine($"Loaded EXISTING Account Key:");
                Console.WriteLine($"  Key Type...........: {accountKey.KeyType}");
                Console.WriteLine($"  Key Export Hash....: {accountKeyHash}");
                Console.WriteLine();
            }

            _http = new HttpClient { BaseAddress = new Uri(url), };
            _acme = new AcmeProtocolClient(_http, acmeDir, account, accountSigner, usePostAsGet: true);
            if (acmeDir == null || RefreshDir)
            {
                Console.WriteLine("Refreshing Service Directory");
                acmeDir = await _acme.GetDirectoryAsync();
                _acme.Directory = acmeDir;
                SaveStateFrom(acmeDir, Constants.AcmeDirectoryFile);
                Console.WriteLine();
            }

            await _acme.GetNonceAsync();

            if (account == null || accountSigner == null)
            {
                Console.WriteLine("Generating/Registering NEW Account Key");
                if (!AcceptTos)
                {
                    Console.WriteLine("You must agree (using CLI flag) to terms of service to create an account:");
                    Console.WriteLine("  " + _acme.Directory.Meta.TermsOfService);
                    return;
                }

                if ((Email?.Count() ?? 0) == 0)
                    throw new Exception("At least one email must be specified as a contact for new account");

                account = await _acme.CreateAccountAsync(Email.Select(x => "mailto:" + x), AcceptTos);
                accountSigner = _acme.Signer;
                accountKey = new ExamplesAccountKey
                {
                    KeyType = accountSigner.JwsAlg,
                    KeyExport = accountSigner.Export(),
                };
                SaveStateFrom(account, Constants.AcmeAccountDetailsFile);
                SaveStateFrom(accountKey, Constants.AcmeAccountKeyFile);
                _acme.Account = account;
            }

            // Dump out Account Details
            var contacts = account.Payload.Contact;
            var contactsJoined = contacts == null ? string.Empty : string.Join(",", contacts);
            Console.WriteLine($"Account Details:");
            Console.WriteLine($"  Id..........: {account?.Payload?.Id}");
            Console.WriteLine($"  Kid.........: {account?.Kid}");
            Console.WriteLine($"  Status......: {account?.Payload?.Status}");
            Console.WriteLine($"  Contacts....: {contactsJoined}");
            // Dump out Account Key
            accountKeyHash = ComputeHash(accountSigner.Export());
            Console.WriteLine($"Account Key:");
            Console.WriteLine($"  JWS Algorithm......: {_acme.Signer.JwsAlg}");
            Console.WriteLine($"  Impl Class Type....: {_acme.Signer.GetType().Name}");
            Console.WriteLine($"  Key Export Hash....: {accountKeyHash}");
            Console.WriteLine();

            if (Dns?.Count() == 0)
                // No DNS names means we can only handle the
                // Account and no request to create an Order
                return;

            Console.WriteLine("################################################################################");
            Console.WriteLine("## ORDER");
            Console.WriteLine("################################################################################");
            Console.WriteLine();

            var dnsNames = Dns.Distinct();
            var certName = string.Join(",", dnsNames.Distinct()).Replace("%", "").ToLower();
            var certNameHash = ComputeHash(certName);
            var orderId = certNameHash;

            OrderDetails order = default;
            LoadStateInto(ref order, failThrow: false,
                    Constants.AcmeOrderDetailsFileFmt, orderId);

            if (order == null)
            {
                Console.WriteLine("Creating NEW Order");
                order = await _acme.CreateOrderAsync(dnsNames);
                SaveStateFrom(order, Constants.AcmeOrderDetailsFileFmt, orderId);
            }

            if (RefreshOrder)
            {
                Console.WriteLine("Refreshing EXISTING Order");
                order = await _acme.GetOrderDetailsAsync(order.OrderUrl, existing: order);
                SaveStateFrom(order, Constants.AcmeOrderDetailsFileFmt, orderId);
            }

            // Dump out Order Details
            Console.WriteLine($"Order Details:");
            Console.WriteLine($"  Order URL......: {order.OrderUrl}");
            Console.WriteLine($"  Expires........: {order.Payload.Expires}");
            Console.WriteLine($"  Status.........: {order.Payload.Status}");
            Console.WriteLine($@"  Identifiers....: {string.Join(",",
                    order.Payload.Identifiers.Select(x => $"{x.Type}:{x.Value}"))}");
            Console.WriteLine();

            if (WaitForTest.enabled)
                _testWaitUntil = DateTime.Now.AddSeconds(
                        WaitForTest.timeout.GetValueOrDefault(300));


            if (order.Payload.Status == Constants.InvalidStatus)
                throw new Exception("Order is already marked as INVALID");
            
            if (order.Payload.Status == Constants.PendingStatus)
            {
                var authzStatusCounts = new Dictionary<string, int>
                {
                    [Constants.ValidStatus] = 0,
                    [Constants.InvalidStatus] = 0,
                    [Constants.ValidStatus] = 0,
                    ["unknown"] = 0,
                };
                void AddStatusCount(string status, int add)
                {
                    if (!authzStatusCounts.ContainsKey(status))
                        status = "unknown";
                    authzStatusCounts[status] += add;
                }

                Console.WriteLine("== Authorizations ==============================================================");
                Console.WriteLine();

                foreach (var authzUrl in order.Payload.Authorizations)
                {
                    var authzId = ComputeHash(authzUrl);
                    Authorization authz = default;
                    LoadStateInto(ref authz, failThrow: false,
                            Constants.AcmeOrderAuthzDetailsFileFmt, orderId, authzId);
                    if (authz == null || RefreshOrder)
                    {
                        Console.WriteLine("Getting Authorization Details...");
                        authz = await _acme.GetAuthorizationDetailsAsync(authzUrl);
                        SaveStateFrom(authz, Constants.AcmeOrderAuthzDetailsFileFmt, orderId, authzId);
                    }

                    Console.WriteLine($"Identifier: [{authz.Identifier.Value}]");
                    Console.WriteLine($"  Status.............: {authz.Status}");
                    Console.WriteLine($"  Expires............: {authz.Expires}");
                    Console.WriteLine($"  Is Wildcard........: {authz.Wildcard}");
                    Console.WriteLine($"  Challenge Count....: {authz.Challenges.Length}");
                    Console.WriteLine();

                    AddStatusCount(authz.Status, 1);

                    Console.WriteLine("-- Challenges ------------------------------------------------------------------");
                    int chlngCount = 0;
                    foreach (var chlng in authz.Challenges)
                    {
                        if (ChallengeType != null
                                && !ChallengeType.Equals(chlng.Type, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine($"  Skipping [{chlng.Type}]");
                            continue;
                        }

                        Authorization authzUpdated = null;
                        Challenge chlngUpdated = null;
                        if (RefreshChallenges)
                        {
                            Console.WriteLine("  Refreshing Challenge...");
                            chlngUpdated = await _acme.GetChallengeDetailsAsync(chlng.Url);
                        }
                        var cd = AuthorizationDecoder.DecodeChallengeValidation(authz, chlng.Type, accountSigner);
                        switch (chlng.Type)
                        {
                            case Dns01ChallengeValidationDetails.Dns01ChallengeType:
                                await ProcessDns01(accountSigner, authz, chlngUpdated ?? chlng, cd);
                                break;
                            case Http01ChallengeValidationDetails.Http01ChallengeType:
                                await ProcessHttp01(accountSigner, authz, chlngUpdated ?? chlng, cd);
                                break;
                            default:
                                Console.WriteLine($"  Challenge of Type: {cd.ChallengeType}");
                                Console.WriteLine($"  This Challenge type is unknown, but here are the details:");
                                Console.WriteLine(JsonConvert.SerializeObject(cd));
                                break;
                        }

                        if (AnswerChallenges)
                        {
                            Console.WriteLine("  Answering Challenge...");
                            chlngUpdated = await _acme.AnswerChallengeAsync(chlng.Url);
                            authzUpdated = await _acme.GetAuthorizationDetailsAsync(authzUrl);
                        }
                        if (chlngUpdated != null)
                        {
                            SaveStateFrom(chlngUpdated, Constants.AcmeOrderAuthzChlngDetailsFileFmt,
                                    orderId, authzId, chlngUpdated.Type);
                        }
                        if (authzUpdated != null)
                        {
                            if (authzUpdated.Status != authz.Status)
                            {
                                AddStatusCount(authz.Status, -1);
                                AddStatusCount(authzUpdated.Status, 1);
                            }
                            authz = authzUpdated;
                            SaveStateFrom(authz, Constants.AcmeOrderAuthzDetailsFileFmt, orderId, authzId);
                            authzUpdated = null;
                        }
                        ++chlngCount;
                    }
                    
                    if (chlngCount == 0)
                        throw new Exception($"No matching Challenges found for Type [{ChallengeType}]");
                }

                if (Finalize)
                {
                    if (authzStatusCounts[Constants.ValidStatus] <
                            order.Payload.Authorizations.Length)
                    {
                        int validStatusCount = 0;
                        if (WaitForAuthz.enabled)
                        {
                            Console.WriteLine("Waiting for Authorizations to be Validated...");
                            var waitUntil = DateTime.Now.AddSeconds(
                                    WaitForAuthz.timeout.GetValueOrDefault(300));
                            foreach (var authzUrl in order.Payload.Authorizations)
                            {
                                var authz = await _acme.GetAuthorizationDetailsAsync(authzUrl);
                                Console.WriteLine($"  Identifier: {authz.Identifier.Type}:{authz.Identifier.Value}:");
                                while (authz.Status != Constants.ValidStatus
                                        && DateTime.Now < waitUntil)
                                {
                                    Thread.Sleep(1 * 1000);
                                    authz = await _acme.GetAuthorizationDetailsAsync(authzUrl);
                                }
                                Console.WriteLine($"    Status: {authz.Status}");
                                if (authz.Status == Constants.ValidStatus)
                                    ++validStatusCount;
                            }
                            Console.WriteLine();
                        }

                        if (validStatusCount < order.Payload.Authorizations.Length)
                            throw new Exception("Cannot finalize Order until all Authorizations are valid");
                    }

                    PkiKeyPair keyPair = null;
                    PkiCertificateSigningRequest csr = null;

                    string certKeys = null;
                    byte[] certCsr = null;

                    if (LoadStateInto(ref certKeys, failThrow: false,
                            Constants.AcmeOrderCertKeyFmt, orderId))
                        Console.WriteLine("Loaded existing Certificate key pair");
                    if (LoadStateInto(ref certCsr, failThrow: false,
                            Constants.AcmeOrderCertCsrFmt, orderId))
                        Console.WriteLine("Loaded existing CSR");

                    if (certKeys == null || certCsr == null || RegenerateCsr)
                    {
                        Console.WriteLine("Generating Certificate key pair and CSR...");
                        switch (KeyAlgor)
                        {
                            case Constants.RsaKeyType:
                                keyPair = PkiKeyPair.GenerateRsaKeyPair(KeySize ?? Constants.DefaultAlgorKeySizeMap[KeyAlgor]);
                                break;
                            case Constants.EcKeyType:
                                keyPair = PkiKeyPair.GenerateEcdsaKeyPair(KeySize ?? Constants.DefaultAlgorKeySizeMap[KeyAlgor]);
                                break;
                            default:
                                throw new Exception($"Unknown key algorithm type [{KeyAlgor}]");
                        }

                        csr = GenerateCsr(Dns, keyPair);
                        certKeys = Save(keyPair);
                        certCsr = csr.ExportSigningRequest(PkiEncodingFormat.Der);

                        SaveStateFrom(certKeys, Constants.AcmeOrderCertKeyFmt, orderId);
                        SaveStateFrom(certCsr, Constants.AcmeOrderCertCsrFmt, orderId);
                    }

                    Console.WriteLine("Finalizing Order...");
                    order = await _acme.FinalizeOrderAsync(order.Payload.Finalize, certCsr);
                    SaveStateFrom(order, Constants.AcmeOrderDetailsFileFmt, orderId);
                }
                else
                {
                    // Since we haven't been asked to Finalize and we were most recently
                    // still in PENDING state, we should end at this point to give it time
                    return;
                }
            }
            
            if (order.Payload.Status == Constants.ValidStatus)
            {
                Console.WriteLine("Order is VALID");


                if (string.IsNullOrEmpty(order.Payload.Certificate))
                {
                    if (WaitForCert.enabled)
                    {
                        Console.WriteLine("Waiting for Certificate to become available");
                        var waitUntil = DateTime.Now.AddSeconds(
                                WaitForCert.timeout.GetValueOrDefault(300));
                        while (DateTime.Now < waitUntil)
                        {
                            Console.WriteLine("    Waiting...");
                            Thread.Sleep(10 * 1000);
                            order = await _acme.GetOrderDetailsAsync(order.OrderUrl, existing: order);

                            if (RefreshOrder)
                            {
                                // If refresh was indicated, persist the latest state
                                SaveStateFrom(order, Constants.AcmeOrderDetailsFileFmt, orderId);
                            }

                            if (!string.IsNullOrEmpty(order.Payload.Certificate))
                                break;
                        }
                    }
                    else
                    {
                        throw new Exception("Certificate URL on Order is missing, did you refresh or wait long enough?");
                    }
                }

                if (RefreshCert || !StateExists(Constants.AcmeOrderCertFmt, orderId))
                {
                    Console.WriteLine("Fetching Certificate...");
                    var certResp = await _acme.GetAsync(order.Payload.Certificate);
                    certResp.EnsureSuccessStatusCode();
                    using (var ras = await certResp.Content.ReadAsStreamAsync())
                    {
                        SaveRaw(ras, Constants.AcmeOrderCertFmt, orderId);
                    }
                }
                else
                {
                    Console.WriteLine("Certificate already cached");
                }
                
                if (ExportCert != null)
                {
                    Console.WriteLine("Exporting Certificate Copy...");
                    using (var rs = LoadRaw<Stream>(true, Constants.AcmeOrderCertFmt, orderId))
                    using (var ws = new FileStream(ExportCert, FileMode.Create))
                    {
                        await rs.CopyToAsync(ws);
                    }
                }

                if (ExportPfx != null)
                {
                    Console.WriteLine("Exporting Certificate as PFX (PKCS12)...");

                    PkiKey privateKey = null;
                    var pfxPassword = ExportPfxPassword;
                    if (pfxPassword != null)
                    {
                        Console.WriteLine("...including private key in export");
                        string certKeys = default;
                        LoadStateInto(ref certKeys, failThrow: true,
                            Constants.AcmeOrderCertKeyFmt, orderId);
                        var keyPair = Load(certKeys);
                        privateKey = keyPair.PrivateKey;
                        if (pfxPassword == " ")
                        {
                            Console.WriteLine("...WITH NO PASSWORD");
                            pfxPassword = null;
                        }
                        else
                        {
                            Console.WriteLine("...securing with password");
                        }
                    }

                    using (var cert = new X509Certificate2(LoadRaw<byte[]>(true, Constants.AcmeOrderCertFmt, orderId)))
                    {
                        var pkiCert = PkiCertificate.From(cert);
                        var pfx = pkiCert.Export(PkiArchiveFormat.Pkcs12,
                            privateKey: privateKey,
                            password: pfxPassword?.ToCharArray());

                        await File.WriteAllBytesAsync(ExportPfx, pfx);
                    }
                }
            }
            else
            {
                throw new Exception($"Order is in unexpected state [{order.Payload.Status}];"
                        + $" expected [{Constants.PendingStatus}]");
            }
        }

        private async Task ProcessDns01(IJwsTool accountSigner, Authorization authz,
            Challenge chlng, IChallengeValidationDetails cd)
        {
            var dnsCd = (Dns01ChallengeValidationDetails)cd;
            var dnsCdValue = dnsCd.DnsRecordValue;
            Console.WriteLine($"  Challenge of Type: [{dnsCd.ChallengeType}]");
            Console.WriteLine($"    To handle this Challenge, create a DNS record with these details:");
            Console.WriteLine($"        DNS Record Name.....: {dnsCd.DnsRecordName}");
            Console.WriteLine($"        DNS Record Type.....: {dnsCd.DnsRecordType}");
            Console.WriteLine($"        DNS Record Value....: {dnsCdValue}");
            Console.WriteLine();

            if (chlng.Status != Constants.PendingStatus)
            {
                Console.WriteLine($"    Challenge No Longer Pending [{chlng.Status}]");
            }
            else if (TestChallenges)
            {
                Console.WriteLine("    Testing for handling of DNS Challenge");

                while (true)
                {
                    string err = null;
                    var lookup = await DnsUtil.LookupRecordAsync(dnsCd.DnsRecordType, dnsCd.DnsRecordName);
                    var dnsValues = lookup?.Select(x => x.Trim('"'));
                    if (dnsValues == null)
                    {
                        err = "Could not resolve *any* DNS entries for Challenge record name";
                    }
                    else if (!dnsValues.Contains(dnsCdValue))
                    {
                        var dnsValuesFlattened = string.Join(",", dnsValues);
                        err = $"DNS entry does not match expected value for Challenge record name ({dnsCdValue} not in {dnsValuesFlattened})";
                    }
                    else
                    {
                        Console.WriteLine("        Found response:");
                        foreach (var dv in dnsValues)
                            Console.WriteLine($"          {dv}");
                        Console.WriteLine("    SUCCESS!  Found expected DNS entry for Challenge record name");
                        // We're done
                        break;
                    }

                    if (DateTime.Now < _testWaitUntil.GetValueOrDefault(DateTime.MinValue))
                    {
                        Console.WriteLine("        Last Test:  " + err);
                        Console.WriteLine("        Waiting...");
                        Thread.Sleep(30 * 1000);
                        continue;
                    }

                    throw new Exception(err);
                }
            }
            Console.WriteLine();
        }

        private async Task ProcessHttp01(IJwsTool accountSigner, Authorization authz,
            Challenge chlng, IChallengeValidationDetails cd)
        {
            var httpCd = (Http01ChallengeValidationDetails)cd;
            Console.WriteLine($"  Challenge of Type: [{httpCd.ChallengeType}]");
            Console.WriteLine($"    To handle this Challenge, create a file that will respond to an HTTP request with these details:");
            Console.WriteLine($"        HTTP Full URL.................: {httpCd.HttpResourceUrl}");
            Console.WriteLine($"        HTTP Resource Path............: {httpCd.HttpResourcePath}");
            Console.WriteLine($"        HTTP Resource Value...........: {httpCd.HttpResourceValue}");
            Console.WriteLine($"        HTTP Resource Content-Type....: {httpCd.HttpResourceContentType}");

            if (chlng.Status != Constants.PendingStatus)
            {
                Console.WriteLine($"    Challenge No Longer Pending [{chlng.Status}]");
            }
            else if (TestChallenges)
            {
                Console.WriteLine("    Testing for handling of HTTP Challenge");

                while (true)
                {
                    string err = null;
                    var httpValue = await HttpUtil.GetStringAsync(httpCd.HttpResourceUrl);
                    if (string.IsNullOrEmpty(httpValue))
                    {
                        err = "Missing or empty HTTP response for Challenge URL";
                    }
                    else if (httpValue != httpCd.HttpResourceValue)
                    {
                        err = "HTTP response content does not match expected value for Challenge URL";
                    }
                    else
                    {
                        Console.WriteLine("        Found response:");
                        Console.WriteLine($"          {httpValue}");
                        Console.WriteLine("    SUCCESS!  Found expected HTTP response content for Challenge URL");
                        // We're done
                        break;
                    }

                    if (DateTime.Now < _testWaitUntil.GetValueOrDefault(DateTime.MinValue))
                    {
                        Console.WriteLine("        Last Test:  " + err);
                        Console.WriteLine("        Waiting...");
                        Thread.Sleep(30 * 1000);
                        continue;
                    }

                    throw new Exception(err);
                }
            }
            Console.WriteLine();
        }

        private bool StateExists(string nameFormat, params object[] nameArgs)
        {
            var name = string.Format(nameFormat, nameArgs);
            var fullPath = Path.Combine(_statePath, name);
            return File.Exists(fullPath);
        }

        private bool SaveStateFrom<T>(T value, string nameFormat, params object[] nameArgs)
        {
            var name = string.Format(nameFormat, nameArgs);
            var fullPath = Path.Combine(_statePath, name);
            var fullDir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);

            var ser = JsonConvert.SerializeObject(value, Formatting.Indented);
            File.WriteAllText(fullPath, ser);
            return true;
        }

        private bool SaveRaw<T>(T value, string nameFormat, params object[] nameArgs)
        {
            var name = string.Format(nameFormat, nameArgs);
            var fullPath = Path.Combine(_statePath, name);
            var fullDir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(fullDir))
                Directory.CreateDirectory(fullDir);

            switch (value)
            {
                case string s:
                    File.WriteAllText(fullPath, s);
                    break;
                case byte[] b:
                    File.WriteAllBytes(fullPath, b);
                    break;
                case Stream m:
                    using (var fs = new FileStream(fullPath, FileMode.Create))
                        m.CopyTo(fs);
                    break;
                default:
                    throw new ArgumentException("Unsupported value type; must be one of:  string, byte[], Stream",
                            nameof(value));
            }

            return true;
        }

        private bool LoadStateInto<T>(ref T value, string nameFormat, params object[] nameArgs) =>
                LoadStateInto<T>(ref value, true, nameFormat, nameArgs);

        private bool LoadStateInto<T>(ref T value, bool failThrow, string nameFormat, params object[] nameArgs)
        {
            var name = string.Format(nameFormat, nameArgs);
            var fullPath = Path.Combine(_statePath, name);
            if (!File.Exists(fullPath))
                if (failThrow)
                    throw new Exception($"Failed to read object from non-existent path [{fullPath}]");
                else
                    return false;
            
            var ser = File.ReadAllText(fullPath);
            value = JsonConvert.DeserializeObject<T>(ser);
            return true;
        }

        private T LoadRaw<T>(bool failThrow, string nameFormat, params object[] nameArgs)
        {
            var name = string.Format(nameFormat, nameArgs);
            var fullPath = Path.Combine(_statePath, name);
            if (!File.Exists(fullPath))
                if (failThrow)
                    throw new Exception($"Failed to read object from non-existent path [{fullPath}]");
                else
                    return default;

            if (typeof(T) == typeof(string))
                return (T)(object)File.ReadAllText(fullPath);
            else if (typeof(T) == typeof(byte[]))
                return (T)(object)File.ReadAllBytes(fullPath);
            else if (typeof(T) == typeof(Stream) || typeof(T) == typeof(FileStream))
                return (T)(object)new FileStream(fullPath, FileMode.Open);
            else
                throw new ArgumentException("Unsupported return type; must be one of:  string, byte[], Stream",
                        nameof(T));
        }

        private string ComputeHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        private string Save(PkiKeyPair keyPair)
        {
            using (var ms = new MemoryStream())
            {
                keyPair.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private PkiKeyPair Load(string b64)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(b64)))
            {
                return PkiKeyPair.Load(ms);
            }
        }

        private PkiCertificateSigningRequest GenerateCsr(IEnumerable<string> dnsNames,
            PkiKeyPair keyPair)
        {
            var firstDns = dnsNames.First();
            var csr = new PkiCertificateSigningRequest($"CN={firstDns}", keyPair,
                PkiHashAlgorithm.Sha256);

            csr.CertificateExtensions.Add(
                PkiCertificateExtension.CreateDnsSubjectAlternativeNames(dnsNames));

            return csr;
        }
    }
}
