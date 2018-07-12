using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using Org.BouncyCastle.Crypto.Parameters;
using PKISharp.SimplePKI;

namespace ACMEBlazor
{
    public class PkiJwsTool : IJwsTool
    {
        private PkiKeyPair _keys;

        private object _jwk;

        public PkiJwsTool(int bits)
        {
            Bits = bits;
        }

        public int Bits { get; }

        public string JwsAlg => $"ES{Bits}";

        public PkiKeyPair KeyPair => _keys;

        ///// <summary>
        ///// Specifies the elliptic curve to use.
        ///// </summary>
        ///// <returns></returns>
        //public ECCurve Curve { get; private set; }

        ///// <summary>
        ///// As per:  https://tools.ietf.org/html/rfc7518#section-6.2.1.1
        ///// </summary>
        //public string CurveName { get; private set; }

        public void Init()
        {
            //switch (Bits)
            //{
            //    case 256:
            //        Curve = ECCurve.NamedCurves.nistP256;
            //        CurveName = "P-256";
            //        break;
            //    case 384:
            //        Curve = ECCurve.NamedCurves.nistP384;
            //        CurveName = "P-384";
            //        break;
            //    case 512:
            //        Curve = ECCurve.NamedCurves.nistP521;
            //        CurveName = "P-521";
            //        break;
            //    default:
            //        throw new System.InvalidOperationException("illegal SHA2 hash size");
            //}

            _keys = PkiKeyPair.GenerateEcdsaKeyPair(Bits);
        }

        public void Dispose()
        {
            _keys = null;
        }

        public byte[] Sign(byte[] raw)
        {
            return _keys.Sign(raw);
        }

        public bool Verify(byte[] raw, byte[] sig)
        {
            return _keys.Verify(raw, sig);
        }

        public string Export()
        {
            using (var ms = new MemoryStream())
            {
                _keys.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public void Import(string exported)
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(exported)))
            {
                _keys = PkiKeyPair.Load(ms);
            }
        }

        public object ExportJwk(bool canonical = false)
        {
            // Note, we only produce a canonical form of the JWK
            // for export therefore we ignore the canonical param

            if (_jwk == null)
            {
                _jwk = _keys.ExportJwk();
            }

            return _jwk;
        }
    }
}
