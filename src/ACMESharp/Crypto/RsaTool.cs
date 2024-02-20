using System.Security.Cryptography;
using System.Text.Json;

namespace ACMESharp.Crypto
{
    public static partial class CryptoHelper
    {

        /// <summary>
        /// Collection of convenient crypto operations working
        /// with RSA keys and algorithms.
        /// </summary>
        public static class Rsa
        {
            public static RSA GenerateAlgorithm(string rsaKeys)
            {
                RSA rsa = RSA.Create();
                var keys = JsonSerializer.Deserialize<RsaKeys>(rsaKeys, JsonHelpers.JsonWebOptions);
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

            public static string GenerateKeys(RSA rsa)
            {
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
                return JsonSerializer.Serialize(keys, JsonHelpers.JsonWebOptions);
            }

            // We have to create our own structure to represent an export of RSA Keys because
            // there is no standard format or method supported for .NET Standard/Core yet.
            //    https://github.com/dotnet/corefx/issues/23686
            //    https://gist.github.com/Jargon64/5b172c452827e15b21882f1d76a94be4/
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
        }
    }
}