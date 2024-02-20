using System;
using System.Security.Cryptography;

namespace ACMESharp.Crypto
{
    public static partial class CryptoHelper
    {

        /// <summary>
        /// Collection of convenient crypto operations working
        /// with Elliptic Curve keys and algorithms.
        /// </summary>
        public static partial class Ec
        {
            public static ECDsa GenerateAlgorithm(int curveSize)
            {
                var curve = curveSize switch
                {
                    256 => ECCurve.NamedCurves.nistP256,
                    384 => ECCurve.NamedCurves.nistP384,
                    _ => throw new ArgumentOutOfRangeException(nameof(curveSize), "only 256 and 384 curves are supported"),
                };
                return ECDsa.Create(curve);
            }

            public static ECDsa GenerateAlgorithm(string ecKeys)
            {
                var dsa = ECDsa.Create();
                dsa.FromXmlString(ecKeys);
                return dsa;
            }

            public static string GenerateKeys(int curveSize)
            {
                return GenerateKeys(GenerateAlgorithm(curveSize));
            }

            public static string GenerateKeys(ECDsa ec)
            {
                return ec.ToXmlString(true);
            }
        }
    }
}