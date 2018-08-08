using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace ACMESharp.Crypto.JOSE.Impl
{
    /// <summary>
    /// JWS Signing tool implements RS-family of algorithms as per
    /// http://self-issued.info/docs/draft-ietf-jose-json-web-algorithms-00.html#SigAlgTable
    /// </summary>
    public class RSJwsTool : IJwsTool
    {
        private HashAlgorithm _sha;
        private RSACryptoServiceProvider _rsa;
        private object _jwk;

        /// <summary>
        /// Specifies the size in bits of the SHA-2 hash function to use.
        /// Supported values are 256, 384 and 512.
        /// </summary>
        public int HashSize { get; set; } = 256;

        /// <summary>
        /// Specifies the size in bits of the RSA key to use.
        /// Supports values in the range 2048 - 4096 inclusive.
        /// </summary>
        /// <returns></returns>
        public int KeySize { get; set; } = 2048;

        public string JwsAlg => $"RS{HashSize}";

        public void Init()
        {
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

        public string Export()
        {
            return _rsa.ToXmlString(true);
        }

        public void Import(string exported)
        {
            _rsa.FromXmlString(exported);
        }

        // public void Save(Stream stream)
        // {
        //     using (var w = new StreamWriter(stream))
        //     {
        //         w.Write(_rsa.ToXmlString(true));
        //     }
        // }

        // public void Load(Stream stream)
        // {
        //     using (var r = new StreamReader(stream))
        //     {
        //         _rsa.FromXmlString(r.ReadToEnd());
        //     }
        // }

        public object ExportJwk(bool canonical = false)
        {
            // Note, we only produce a canonical form of the JWK
            // for export therefore we ignore the canonical param

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

        public void ImportJwk(string jwkJson)
        {
            Init();
            var jwk = JsonConvert.DeserializeObject<JwkExport>(jwkJson);
            var keyParams = new RSAParameters
            {
                Exponent = CryptoHelper.Base64.UrlDecode(jwk.e),
                Modulus = CryptoHelper.Base64.UrlDecode(jwk.n),
            };
            _rsa.ImportParameters(keyParams);
        }

        public byte[] Sign(byte[] raw)
        {
            return _rsa.SignData(raw, _sha);
        }

        public bool Verify(byte[] raw, byte[] sig)
        {
            return _rsa.VerifyData(raw, _sha, sig);
        }

        public class JwkExport
        {
            // As per RFC 7638 Section 3, these are the *required* elements of the
            // JWK and are sorted in lexicographic order to produce a canonical form

            public string e { get; set; }

            public string kty { get; set; }

            public string n { get; set; }
        }
    }
}
