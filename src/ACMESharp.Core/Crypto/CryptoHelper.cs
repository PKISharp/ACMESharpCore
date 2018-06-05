using System;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ACMESharp.Crypto
{
    /// <summary>
    /// For the most compatibility with LE, see:
    ///   https://letsencrypt.org/docs/integration-guide/#supported-key-algorithms
    /// We should support:
    /// * RSA Keys (2048-4096 bits)
    /// * ECDSA Keys (P-256, P-384)
    /// 
    /// Thats' for both account keys and cert keys.
    /// </summary>
    public static class CryptoHelper
    {
        public static RSA GenerateRsaAlgorithm(int keyBitLength)
        {
            return RSA.Create(keyBitLength);
        }

        public static RSA GenerateRsaAlgorithm(string rsaKeys)
        {
            var rsa = RSA.Create();
            var keys = JsonConvert.DeserializeObject<RsaKeys>(rsaKeys);
            var rsaParams = new RSAParameters
            {
                D = keys.D,
                DP = keys.DP,
                DQ = keys.DQ,
                Exponent = keys.Exponent,
                InverseQ = keys.InverseQ,
                Modulus = keys.Modulus,
                P = keys.P,
                Q = keys.Q,
            };
            rsa.ImportParameters(rsaParams);
            return rsa;
        }

        public static string GenerateRsaKeys(int keyBitLength)
        {
            var rsa = GenerateRsaAlgorithm(keyBitLength);
            var rsaParams = rsa.ExportParameters(true);
            var keys = new RsaKeys
            {
                D = rsaParams.D,
                DP = rsaParams.DP,
                DQ = rsaParams.DQ,
                Exponent = rsaParams.Exponent,
                InverseQ = rsaParams.InverseQ,
                Modulus = rsaParams.Modulus,
                P = rsaParams.P,
                Q = rsaParams.Q,
            };
            var json = JsonConvert.SerializeObject(keys);
            return json;
        }

        // https://github.com/dotnet/corefx/issues/23686
        // https://gist.github.com/Jargon64/5b172c452827e15b21882f1d76a94be4/
        private class RsaKeys
        {
            public byte[] Modulus { get; set; }
            public byte[] Exponent { get; set; }
            public byte[] P { get; set; }
            public byte[] Q { get; set; }
            public byte[] DP { get; set; }
            public byte[] DQ { get; set; }
            public byte[] InverseQ { get; set; }
            public byte[] D { get; set; }
        }

        public static ECDsa GenerateEcAlgorithm(int curveSize)
        {
            ECCurve curve;
            switch (curveSize)
            {
                case 256:
                    curve = ECCurve.NamedCurves.nistP256;
                    break;
                case 384:
                    curve = ECCurve.NamedCurves.nistP384;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("only 256 and 384 curves are supported");
            }

            return ECDsa.Create(curve);
        }

        public static ECDsa GenerateEcAlgorithm(string ecKeys)
        {
            var dsa = ECDsa.Create();
            dsa.FromXmlString(ecKeys);
            return dsa;
        }

        public static string GenerateEcKeys(int curveSize)
        {
            return GenerateEcAlgorithm(curveSize).ToXmlString(true);;
        }

        /// <summary>
        /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static string Base64UrlEncode(string raw, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return Base64UrlEncode(encoding.GetBytes(raw));
        }

        /// <summary>
        /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static string Base64UrlEncode(byte[] raw)
        {
            string enc = Convert.ToBase64String(raw);  // Regular base64 encoder
            enc = enc.Split('=')[0];                   // Remove any trailing '='s
            enc = enc.Replace('+', '-');               // 62nd char of encoding
            enc = enc.Replace('/', '_');               // 63rd char of encoding
            return enc;
        }

        /// <summary>
        /// URL-safe Base64 decoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static byte[] Base64UrlDecode(string enc)
        {
            string raw = enc;
            raw = raw.Replace('-', '+');  // 62nd char of encoding
            raw = raw.Replace('_', '/');  // 63rd char of encoding
            switch (raw.Length % 4)       // Pad with trailing '='s
            {
                case 0: break;               // No pad chars in this case
                case 2: raw += "=="; break;  // Two pad chars
                case 3: raw += "="; break;   // One pad char
                default:
                    throw new System.Exception("Illegal base64url string!");
            }
            return Convert.FromBase64String(raw); // Standard base64 decoder
        }

        public static string Base64UrlDecodeToString(string enc, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return encoding.GetString(Base64UrlDecode(enc));
        }

        /// <summary>
        /// Returns a DER-encoded PKCS#10 Certificate Signing Request for the given RSA parametes
        /// and the given hash algorithm.
        /// </summary>
        public static byte[] GenerateCsr(IEnumerable<string> dnsNames, RSA rsa, HashAlgorithmName? hashAlgor = null)
        {
            if (hashAlgor == null)
                hashAlgor = HashAlgorithmName.SHA256;

            string firstName = null;
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var n in dnsNames)
            {
                sanBuilder.AddDnsName(n);
                if (firstName == null)
                    firstName = n;
            }
            if (firstName == null)
                throw new ArgumentException("Must specify at least one name");

            var dn = new X500DistinguishedName($"CN={firstName}");
            var csr = new CertificateRequest(dn,
                    rsa, hashAlgor.Value, RSASignaturePadding.Pkcs1);
            csr.CertificateExtensions.Add(sanBuilder.Build());

            return csr.CreateSigningRequest();
        }


        /// <summary>
        /// Returns a DER-encoded PKCS#10 Certificate Signing Request for the given ECDsa parametes
        /// and the given hash algorithm.
        /// </summary>
        public static byte[] GenerateCsr(IEnumerable<string> dnsNames, ECDsa dsa, HashAlgorithmName? hashAlgor = null)
        {
            if (hashAlgor == null)
                hashAlgor = HashAlgorithmName.SHA256;

            string firstName = null;
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var n in dnsNames)
            {
                sanBuilder.AddDnsName(n);
                if (firstName == null)
                    firstName = n;
            }
            if (firstName == null)
                throw new ArgumentException("Must specify at least one name");

            var dn = new X500DistinguishedName($"CN={firstName}");
            var csr = new CertificateRequest(dn, dsa, hashAlgor.Value);
            csr.CertificateExtensions.Add(sanBuilder.Build());

            return csr.CreateSigningRequest();
        }
    }
}