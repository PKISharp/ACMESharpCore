using System;
using System.Security.Cryptography;

namespace ACMESharp.Crypto
{
    /// <summary>
    /// Collection of convenient crypto operations working
    /// with Elliptic Curve keys and algorithms.
    /// </summary>
    public class EcTool
    {
        public ECDsa GenerateAlgorithm(int curveSize)
        {
            ECCurve curve;
            switch (curveSize)
            {
                case 256:
                    curve = ECCurve.NamedCurves.nistP256;
                    break;
                case 384:
                    curve = ECCurve.NamedCurves.nistP384;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("only 256 and 384 curves are supported");
            }

            return ECDsa.Create(curve);
        }

        public ECDsa GenerateAlgorithm(string ecKeys)
        {
            var dsa = ECDsa.Create();
            dsa.FromXmlString(ecKeys);
            return dsa;
        }

        public string GenerateKeys(int curveSize)
        {
            return GenerateKeys(GenerateAlgorithm(curveSize));
        }

        public string GenerateKeys(ECDsa ec)
        {
            return ec.ToXmlString(true);
        }
    }
}