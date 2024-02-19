using System;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        private ESJwk _jwk;

        /// <summary>
        /// Specifies the size in bits of the SHA-2 hash function to use.
        /// Supported values are 256, 384 and 512.
        /// </summary>
        public int HashSize { get; set; } = 256;

        /// <summary>
        /// Specifies the elliptic curve to use.
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

        public string Export()
        {
            var ecParams = _dsa.ExportParameters(true);
            var details = new ExportDetails
            {
                HashSize = HashSize,
                D = Convert.ToBase64String(ecParams.D),
                X = Convert.ToBase64String(ecParams.Q.X),
                Y = Convert.ToBase64String(ecParams.Q.Y),
            };
            return JsonSerializer.Serialize<ExportDetails>(details, JsonHelpers.JsonWebOptions);
        }

        public void Import(string exported)
        {
            // TODO: this is inefficient and corner cases exist that will break this -- FIX THIS!!!

            var details = JsonSerializer.Deserialize<ExportDetails>(exported, JsonHelpers.JsonWebOptions);
            HashSize = details.HashSize;
            Init();

            var ecParams = _dsa.ExportParameters(true);
            ecParams.D = Convert.FromBase64String(details.D);
            ecParams.Q.X = Convert.FromBase64String(details.X);
            ecParams.Q.Y = Convert.FromBase64String(details.Y);
            _dsa.ImportParameters(ecParams);

        }

        public object ExportJwk(bool canonical = false)
        {
            // Note, we only produce a canonical form of the JWK
            // for export therefore we ignore the canonical param

            if (_jwk == null)
            {
                var keyParams = _dsa.ExportParameters(false);
                _jwk = new ESJwk
                {
                    crv = CurveName,
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

        public bool Verify(byte[] raw, byte[] sig)
        {
            return _dsa.VerifyData(raw, sig, _shaName);
        }

        /// <summary>
        /// Format for an internal representation of string-based export/import.
        /// </summary>
        class ExportDetails
        {
            public int HashSize { get; set; }

            public string D { get; set; }

            public string X { get; set; }

            public string Y { get; set; }
        }

        // As per RFC 7638 Section 3, these are the *required* elements of the
        // JWK and are sorted in lexicographic order to produce a canonical form
        class ESJwk
        {
            [JsonPropertyOrder(1)]
            public string crv { get; set; }

            [JsonPropertyOrder(2)]
            public string kty { get; set; } = "EC";

            [JsonPropertyOrder(3)]
            public string x { get; set; }

            [JsonPropertyOrder(4)]
            public string y { get; set; }
        }
    }
}