using System;
using System.IO;
using System.Security.Cryptography;

namespace ACMESharp.Crypto.JOSE.Impl
{
    /// <summary>
    /// JWS Signing tool implements RS-family of algorithms as per
    /// http://self-issued.info/docs/draft-ietf-jose-json-web-algorithms-00.html#SigAlgTable
    /// </summary>
    internal class RSJwsSigner : IJwsSigner
    {
        private HashAlgorithm _sha;
        private RSACryptoServiceProvider _rsa;

        private object _jwk;

        /// <summary>
        /// Specifies the size in bits of the SHA-2 hash function to use.
        /// Supported values are 256, 384 and 512.
        /// </summary>
        private int HashSize { get; set; }

        /// <summary>
        /// Specifies the size in bits of the RSA key to use.
        /// Supports values in the range 2048 - 4096 inclusive.
        /// </summary>
        /// <returns></returns>
        public int KeySize { get; set; } = 2048;

        public string JwsAlg => $"RS{HashSize}";

        public RSJwsSigner(int hashSize)
        {
            HashSize = hashSize;

            switch (HashSize)
            {
                case 256:
                    _sha = SHA256.Create();
                    break;
                case 384:
                    _sha = SHA384.Create();
                    break;
                case 512:
                    _sha = SHA512.Create();
                    break;
                default:
                    throw new System.InvalidOperationException("illegal SHA2 hash size");
            }

            if (KeySize < 2048 || KeySize > 4096)
                throw new InvalidOperationException("illegal RSA key bit length");

            _rsa = new RSACryptoServiceProvider(KeySize);
        }

        public void Dispose()
        {
            _rsa?.Dispose();
            _rsa = null;
            _sha?.Dispose();
            _sha = null;
        }

        public string ExportPrivateJwk()
        {
            return _rsa.ToXmlString(true);
        }

        public void Import(string exported)
        {
            _rsa.FromXmlString(exported);
        }
        

        public object ExportPublicJwk()
        {
            if (_jwk == null)
            {
                var keyParams = _rsa.ExportParameters(false);
                _jwk = new
                {
                    // As per RFC 7638 Section 3, these are the *required* elements of the
                    // JWK and are sorted in lexicographic order to produce a canonical form

                    e = CryptoHelper.Base64.UrlEncode(keyParams.Exponent),
                    kty = "RSA", // https://tools.ietf.org/html/rfc7518#section-6.3
                    n = CryptoHelper.Base64.UrlEncode(keyParams.Modulus),
                };
            }

            return _jwk;
        }

        public byte[] Sign(byte[] raw)
        {
            return _rsa.SignData(raw, _sha);
        }
    }
}
