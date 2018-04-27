using System.IO;
using System.Security.Cryptography;

namespace ACMESharp.Crypto.JOSE.Impl
{
    /// <summary>
    /// JWS Signing tool implements ES-family of algorithms as per
    /// http://self-issued.info/docs/draft-ietf-jose-json-web-algorithms-00.html#SigAlgTable
    /// </summary>
    public class ESJwsTool : IJwsTool
    {
        private HashAlgorithmName _shaName;
        private ECDsa _dsa;
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
        public ECCurve Curve { get; private set; }
        /// <summary>
        /// As per:  https://tools.ietf.org/html/rfc7518#section-6.2.1.1
        /// </summary>
        public string CurveName { get; private set; }

        public string JwsAlg => $"ES{HashSize}";

        public void Init()
        {
            switch (HashSize)
            {
                case 256:
                    _shaName = HashAlgorithmName.SHA256;
                    Curve = ECCurve.NamedCurves.nistP256;
                    CurveName = "P-256";
                    break;
                case 384:
                    _shaName = HashAlgorithmName.SHA384;
                    Curve = ECCurve.NamedCurves.nistP384;
                    CurveName = "P-384";
                    break;
                case 512:
                    _shaName = HashAlgorithmName.SHA512;
                    Curve = ECCurve.NamedCurves.nistP521;
                    CurveName = "P-521";
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

        public void Save(Stream stream)
        {
            using (var w = new StreamWriter(stream))
            {
                w.Write(_dsa.ToXmlString(true));
            }
        }

        public void Load(Stream stream)
        {
            using (var r = new StreamReader(stream))
            {
                _dsa.FromXmlString(r.ReadToEnd());
            }
        }


        public object ExportJwk(bool canonical = false)
        {
            // Note, we only produce a canonical form of the JWK
            // for export therefore we ignore the canonical param

            if (_jwk == null)
            {
                var keyParams = _dsa.ExportParameters(false);
                _jwk = new
                {
                    // As per RFC 7638 Section 3, these are the *required* elements of the
                    // JWK and are sorted in lexicographic order to produce a canonical form

                    crv = CurveName,
                    kty = "EC", // https://tools.ietf.org/html/rfc7518#section-6.2
                    x = CryptoHelper.Base64UrlEncode(keyParams.Q.X),
                    y = CryptoHelper.Base64UrlEncode(keyParams.Q.Y),
                };
            }

            return _jwk;
        }

        public byte[] Sign(byte[] raw)
        {
            return _dsa.SignData(raw, _shaName);
        }
    }
}