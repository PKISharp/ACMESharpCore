using System.Security.Cryptography;
using Newtonsoft.Json;

namespace ACMESharp.Crypto
{
    /// <summary>
    /// Collection of convenient crypto operations working
    /// with RSA keys and algorithms.
    /// </summary>
    public class RsaTool
    {
        public RSA GenerateAlgorithm(string rsaKeys)
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

        public string GenerateKeys(RSA rsa)
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
            var json = JsonConvert.SerializeObject(keys);
            return json;
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