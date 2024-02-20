using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ACMESharp.Crypto
{
    public static class RsaHelper
    {
        public static RSA GenerateAlgorithm(int keyBitLength)
        {
            return RSA.Create(keyBitLength);
        }

        public static string GenerateKeys(int keyBitLength)
        {
            var rsa = GenerateAlgorithm(keyBitLength);
            return CryptoHelper.Rsa.GenerateKeys(rsa);
        }

        /// <summary>
        /// Returns a DER-encoded PKCS#10 Certificate Signing Request for the given RSA parametes
        /// and the given hash algorithm.
        /// </summary>
        public static byte[] GenerateCsr(IEnumerable<string> dnsNames,
            RSA rsa, HashAlgorithmName? hashAlgor = null)
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
    }
}
