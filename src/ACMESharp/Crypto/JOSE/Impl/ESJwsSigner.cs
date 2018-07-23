using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ACMESharp.Crypto.JOSE.Impl
{
    /// <summary>
    /// JWS Signing tool implements ES-family of algorithms as per
    /// http://self-issued.info/docs/draft-ietf-jose-json-web-algorithms-00.html#SigAlgTable
    /// </summary>
    internal class ESJwsSigner : JwsAlgorithm
    {
        private HashAlgorithmName _hashName;
        private ECDsa _algorithm;

        private static int[] ValidHashSizes = new[] { 256, 374, 512 };

        /// <summary>
        /// As per:  https://tools.ietf.org/html/rfc7518#section-6.2.1.1
        /// </summary>
        private string CurveName { get; }
        
        public ESJwsSigner(string algorithmIdentifier)
        {
            if (!IsValidIdentifier(algorithmIdentifier))
                throw new ArgumentException("Algorithm name is not valid for this JwsSigner", nameof(algorithmIdentifier));

            var hashSize = ParseAlgorithmIdentifier(algorithmIdentifier);

            AlgorithmIdentifier = algorithmIdentifier;
            JwsAlg = $"ES{hashSize}";
            CurveName = $"P-{hashSize}";

            ECCurve curve;
            switch (hashSize)
            {
                case 256:
                    _hashName = HashAlgorithmName.SHA256;
                    curve = ECCurve.NamedCurves.nistP256;
                    break;
                case 384:
                    _hashName = HashAlgorithmName.SHA384;
                    curve = ECCurve.NamedCurves.nistP384;
                    break;
                case 512:
                    _hashName = HashAlgorithmName.SHA512;
                    curve = ECCurve.NamedCurves.nistP521;
                    break;
                default:
                    //Should never appear.
                    throw new InvalidOperationException("illegal SHA2 hash size");
            }

            _algorithm = ECDsa.Create(curve);
        }

        protected override byte[] SignInternal(byte[] input)
        {
            return _algorithm.SignData(input, _hashName);
        }

        public override object ExportPublicJwk()
        {
            var keyParams = _algorithm.ExportParameters(false);
            return new
            {
                // As per RFC 7638 Section 3, these are the *required* elements of the
                // JWK and are sorted in lexicographic order to produce a canonical form

                crv = CurveName,
                kty = "EC", // https://tools.ietf.org/html/rfc7518#section-6.2
                x = CryptoHelper.Base64.UrlEncode(keyParams.Q.X),
                y = CryptoHelper.Base64.UrlEncode(keyParams.Q.Y),
            };
        }


        protected override string ExportInternal()
        {
            var ecParams = _algorithm.ExportParameters(true);
            var details = new ExportDetails
            {
                D = Convert.ToBase64String(ecParams.D),
                X = Convert.ToBase64String(ecParams.Q.X),
                Y = Convert.ToBase64String(ecParams.Q.Y)
            };
            
            return JsonConvert.SerializeObject(details);
        }

        protected override void ImportInternal(string exported)
        {
            // TODO: this is inefficient and corner cases exist that will break this -- FIX THIS!!!
            var details = JsonConvert.DeserializeObject<ExportDetails>(exported);

            var ecParams = _algorithm.ExportParameters(true);
            ecParams.D = Convert.FromBase64String(details.D);
            ecParams.Q.X = Convert.FromBase64String(details.X);
            ecParams.Q.Y = Convert.FromBase64String(details.Y);
            _algorithm.ImportParameters(ecParams);
        }

        protected override bool IsValidIdentifier(string algorithmIdentifier) => IsValidName(algorithmIdentifier);


        public override void Dispose()
        {
            _algorithm?.Dispose();
            _algorithm = null;
        }


        class ExportDetails
        {
            public string D { get; set; }

            public string X { get; set; }

            public string Y { get; set; }
        }


        private static Regex ValidNameRegex = new Regex("^ES(?'hashSize'\\d{3})$", RegexOptions.Compiled);

        public static bool IsValidName(string algorithmName)
        {
            try
            {
                ParseAlgorithmIdentifier(algorithmName);

                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }
        
        private static int ParseAlgorithmIdentifier(string algorithmIdentifier)
        {
            var match = ValidNameRegex.Match(algorithmIdentifier);
            if (!match.Success)
                throw new ArgumentException($"Could not parse algorithm identifier. It needs to match the following Regex: \"{ValidNameRegex}\"", nameof(algorithmIdentifier));

            var hashSize = int.Parse(match.Groups["hashSize"].Value);

            if (!ValidHashSizes.Contains(hashSize))
                throw new ArgumentOutOfRangeException($"HashSize needs to be one of {string.Join(", ", ValidHashSizes)}", nameof(hashSize));
            
            return hashSize;
        }
    }
}