using System;
using System.IO;
using System.Linq;
using PKISharp.SimplePKI;

namespace ACMESharp.MockServer
{
    public class CertificateAuthority
    {
        private readonly Options _opts;
        private PkiKeyPair _keyPair;

      //private long _serNum;

        public CertificateAuthority(Options opts)
        {
            _opts = opts;

            Init();
        }

        public PkiCertificate CaCertificate { get; set; }

        private void Init()
        {
            if (File.Exists(_opts.CaKeyPairSavePath))
            {
                using var fs = new FileStream(_opts.CaKeyPairSavePath, FileMode.Open);
                _keyPair = PkiKeyPair.Load(fs);
            }
            if (_keyPair == null)
            {
                switch (_opts.KeyPairAlgorithm)
                {
                    case PkiAsymmetricAlgorithm.Rsa:
                        _keyPair = PkiKeyPair.GenerateRsaKeyPair(_opts.BitLength ?? 2048);
                        break;
                    case PkiAsymmetricAlgorithm.Ecdsa:
                        _keyPair = PkiKeyPair.GenerateEcdsaKeyPair(_opts.BitLength ?? 256);
                        break;
                    default:
                        throw new Exception("unsupported Key Pair Algorithm");
                }

                using var fs = new FileStream(_opts.CaKeyPairSavePath, FileMode.CreateNew);
                _keyPair.Save(fs);
            }

            if (File.Exists(_opts.CaCertificateSavePath))
            {
                using var fs = new FileStream(_opts.CaCertificateSavePath, FileMode.Open);
                CaCertificate = PkiCertificate.Load(fs);
            }
            if (CaCertificate == null)
            {
                var caCsr = new PkiCertificateSigningRequest(
                        _opts.CaSubjectName, _keyPair, _opts.SignatureHashAlgorithm);
                CaCertificate = caCsr.CreateCa(
                        DateTimeOffset.Now.ToUniversalTime(),
                        DateTimeOffset.Now.AddYears(10).ToUniversalTime());

                using var fs = new FileStream(_opts.CaCertificateSavePath, FileMode.CreateNew);
                CaCertificate.Save(fs);
            }
        }

        public PkiCertificate Sign(PkiCertificateSigningRequest csr,
                DateTimeOffset? notBefore = null,
                DateTimeOffset? notAfter = null)
        {
            if (notBefore == null)
                notBefore = _opts.MinBefore;
            if (notAfter == null)
                notAfter = _opts.MinAfter;

            if (notBefore.Value > _opts.MaxBefore)
                notBefore = _opts.MaxBefore;
            if (notBefore.Value < _opts.MinBefore)
                notBefore = _opts.MinBefore;

            if (notAfter.Value > _opts.MaxAfter)
                notAfter = _opts.MaxAfter;
            if (notAfter.Value < _opts.MinAfter)
                notAfter = _opts.MinAfter;

            var serNum = DateTime.Now.Ticks;
            var serNumBytes = BitConverter.GetBytes(serNum);
            if (BitConverter.IsLittleEndian)
                serNumBytes = serNumBytes.Reverse().ToArray();

            return csr.Create(CaCertificate, _keyPair.PrivateKey, notBefore.Value, notAfter.Value, serNumBytes);
        }

        public PkiCertificate Sign(PkiEncodingFormat format, byte[] encodedCsr,
                PkiHashAlgorithm signingHashAlgorithm,
                DateTimeOffset? notBefore = null,
                DateTimeOffset? notAfter = null)
        {
            var csr = new PkiCertificateSigningRequest(format, encodedCsr, signingHashAlgorithm);
            return Sign(csr, notBefore, notAfter);
        }

        public class Options
        {
            public string CaKeyPairSavePath { get; set; }

            public string CaCertificateSavePath { get; set; }

            public PkiAsymmetricAlgorithm KeyPairAlgorithm { get; set; }

            public int? BitLength { get; set; }

            public string CaSubjectName { get; set; }

            public PkiHashAlgorithm SignatureHashAlgorithm { get; set; }

            public DateTimeOffset MinBefore { get; set; } = DateTimeOffset.Now.ToUniversalTime();

            public DateTimeOffset MaxBefore { get; set; } = DateTimeOffset.Now.ToUniversalTime();

            public DateTimeOffset MinAfter { get; set; } = DateTimeOffset.Now.AddMonths(3).ToUniversalTime();

            public DateTimeOffset MaxAfter { get; set; } = DateTimeOffset.Now.AddMonths(3).ToUniversalTime();
        }
    }
}