using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace ACMESharp.Crypto.JOSE.Impl
{
    /// <summary>
    /// JWS Signing tool implements RS-family of algorithms as per
    /// http://self-issued.info/docs/draft-ietf-jose-json-web-algorithms-00.html#SigAlgTable
    /// </summary>
    internal class RSJwsSigner : JwsAlgorithm
    {
        private HashAlgorithmName _hashAlgorithm;
        private RSA _algorithm;

        private static int[] ValidHashSizes = new[] { 256, 384, 512 };

        public RSJwsSigner(string algorithmIdentifier)
        {
            if (!IsValidIdentifier(algorithmIdentifier))
                throw new ArgumentException("Algorithm identifier is not valid for this JwsSigner", nameof(algorithmIdentifier));

            var sizes = ParseAlgorithmIdentifier(algorithmIdentifier);

            AlgorithmIdentifier = algorithmIdentifier;
            JwsAlg = $"RS{sizes.hashSize}";

            switch (sizes.hashSize)
            {
                case 256:
                    _hashAlgorithm = HashAlgorithmName.SHA256;
                    break;

                case 384:
                    _hashAlgorithm = HashAlgorithmName.SHA384;
                    break;

                case 512:
                    _hashAlgorithm = HashAlgorithmName.SHA512;
                    break;
            }

            _algorithm = RSA.Create();
            _algorithm.KeySize = (sizes.keySize);
        }

        protected override byte[] SignInternal(byte[] input)
        {
            return _algorithm.SignData(input, 0, input.Length, _hashAlgorithm, RSASignaturePadding.Pkcs1);
        }

        public override object ExportPublicJwk()
        {
            var keyParams = _algorithm.ExportParameters(false);
            return new
            {
                // As per RFC 7638 Section 3, these are the *required* elements of the
                // JWK and are sorted in lexicographic order to produce a canonical form

                e = CryptoHelper.Base64.UrlEncode(keyParams.Exponent),
                kty = "RSA", // https://tools.ietf.org/html/rfc7518#section-6.3
                n = CryptoHelper.Base64.UrlEncode(keyParams.Modulus),
            };
        }


        protected override string ExportInternal()
        {
            var rsaParams = _algorithm.ExportParameters(true);
            var details = ExportDetails.FromParameters(rsaParams);

            return JsonConvert.SerializeObject(details);
        }

        protected override void ImportInternal(string exported)
        {
            var details = JsonConvert.DeserializeObject<ExportDetails>(exported);

            var rsaParams = _algorithm.ExportParameters(true);
            details.ApplyTo(rsaParams);

            _algorithm.ImportParameters(rsaParams);
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
            public string DP { get; set; }
            public string DQ { get; set; }

            public string Exponent { get; set; }
            public string InverseQ { get; set; }
            public string Modulus { get; set; }

            public string P { get; set; }
            public string Q { get; set; }

            public static ExportDetails FromParameters(RSAParameters rsaParams)
            {
                return new ExportDetails
                {
                    D = Convert.ToBase64String(rsaParams.D),
                    DP = Convert.ToBase64String(rsaParams.DP),
                    DQ = Convert.ToBase64String(rsaParams.DQ),

                    Exponent = Convert.ToBase64String(rsaParams.Exponent),
                    InverseQ = Convert.ToBase64String(rsaParams.InverseQ),
                    Modulus = Convert.ToBase64String(rsaParams.Modulus),

                    P = Convert.ToBase64String(rsaParams.P),
                    Q = Convert.ToBase64String(rsaParams.Q)
                };
            }

            public void ApplyTo(RSAParameters rsaParams)
            {
                rsaParams.D = Convert.FromBase64String(D);
                rsaParams.DP = Convert.FromBase64String(DP);
                rsaParams.DQ = Convert.FromBase64String(DQ);

                rsaParams.Exponent = Convert.FromBase64String(Exponent);
                rsaParams.InverseQ = Convert.FromBase64String(InverseQ);
                rsaParams.Modulus = Convert.FromBase64String(Modulus);

                rsaParams.P = Convert.FromBase64String(P);
                rsaParams.Q = Convert.FromBase64String(Q);
            }
        }


        private static Regex ValidNameRegex = new Regex("^RS(?'hashSize'\\d{3})-(?'keySize'\\d{4})$", RegexOptions.Compiled);

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

        private static (int hashSize, int keySize) ParseAlgorithmIdentifier(string algorithmIdentifier)
        {
            var match = ValidNameRegex.Match(algorithmIdentifier);
            if (!match.Success)
                throw new ArgumentException($"Could not parse algorithm identifier. It needs to match the following Regex: \"{ValidNameRegex}\"", nameof(algorithmIdentifier));

            var hashSize = int.Parse(match.Groups["hashSize"].Value);
            var keySize = int.Parse(match.Groups["keySize"].Value);

            if (!ValidHashSizes.Contains(hashSize))
                throw new ArgumentOutOfRangeException($"HashSize needs to be one of {string.Join(", ", ValidHashSizes)}", nameof(hashSize));

            if (keySize < 2048 && keySize > 4096 && keySize % 8 == 0)
                throw new ArgumentOutOfRangeException($"KeySize needs to be between 2048 and 4096 and divisable by 8", nameof(keySize));

            return (hashSize, keySize);
        }
    }
}
