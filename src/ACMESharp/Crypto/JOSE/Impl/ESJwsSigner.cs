using System;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace ACMESharp.Crypto.JOSE.Impl
{
    /// <summary>
    /// JWS Signing tool implements ES-family of algorithms as per
    /// http://self-issued.info/docs/draft-ietf-jose-json-web-algorithms-00.html#SigAlgTable
    /// </summary>
    internal class ESJwsSigner : IJwsSigner
    {
        private HashAlgorithmName _shaName;
        private ECDsa _dsa;

        private object _jwk;

        /// <summary>
        /// Specifies the size in bits of the SHA-2 hash function to use.
        /// Supported values are 256, 384 and 512.
        /// </summary>
        private int HashSize { get; set; }

        /// <summary>
        /// Specifies the elliptic curve to use.
        /// </summary>
        /// <returns></returns>
        private ECCurve Curve { get;  set; }

        /// <summary>
        /// As per:  https://tools.ietf.org/html/rfc7518#section-6.2.1.1
        /// </summary>
        public string CurveName => $"P-{HashSize}";

        public string JwsAlg => $"ES{HashSize}";

        public ESJwsSigner(int hashSize)
        {
            HashSize = hashSize;

            switch (HashSize)
            {
                case 256:
                    _shaName = HashAlgorithmName.SHA256;
                    Curve = ECCurve.NamedCurves.nistP256;
                    break;
                case 384:
                    _shaName = HashAlgorithmName.SHA384;
                    Curve = ECCurve.NamedCurves.nistP384;
                    break;
                case 512:
                    _shaName = HashAlgorithmName.SHA512;
                    Curve = ECCurve.NamedCurves.nistP521;
                    break;
                default:
                    throw new System.InvalidOperationException("illegal SHA2 hash size");
            }

            _dsa = ECDsa.Create(Curve);
        }

        public void Dispose()
        {
            _dsa?.Dispose();
            _dsa = null;
        }

        public string ExportPrivateJwk()
        {
            var ecParams = _dsa.ExportParameters(true);
            var details = new ExportDetails
            {
                D = Convert.ToBase64String(ecParams.D),
                X = Convert.ToBase64String(ecParams.Q.X),
                Y = Convert.ToBase64String(ecParams.Q.Y),
            };
            return JsonConvert.SerializeObject(details);
        }

        public void Import(string exported)
        {
            // TODO: this is inefficient and corner cases exist that will break this -- FIX THIS!!!

            var details = JsonConvert.DeserializeObject<ExportDetails>(exported);

            var ecParams = _dsa.ExportParameters(true);
            ecParams.D = Convert.FromBase64String(details.D);
            ecParams.Q.X = Convert.FromBase64String(details.X);
            ecParams.Q.Y = Convert.FromBase64String(details.Y);
            _dsa.ImportParameters(ecParams);

        }
        
        public object ExportPublicJwk()
        {
            if (_jwk == null)
            {
                var keyParams = _dsa.ExportParameters(false);
                _jwk = new
                {
                    // As per RFC 7638 Section 3, these are the *required* elements of the
                    // JWK and are sorted in lexicographic order to produce a canonical form

                    crv = CurveName,
                    kty = "EC", // https://tools.ietf.org/html/rfc7518#section-6.2
                    x = CryptoHelper.Base64.UrlEncode(keyParams.Q.X),
                    y = CryptoHelper.Base64.UrlEncode(keyParams.Q.Y),
                };
            }

            return _jwk;
        }
        
        public byte[] Sign(byte[] raw)
        {
            return _dsa.SignData(raw, _shaName);
        }
        
        class ExportDetails
        {
            public string D { get; set; }

            public string X { get; set; }

            public string Y { get; set; }
        }
    }
}