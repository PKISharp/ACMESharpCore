using System;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ACMESharp.Crypto
{
    /// <summary>
    /// For the most compatibility with LE, see:
    ///   https://letsencrypt.org/docs/integration-guide/#supported-key-algorithms
    /// We should support:
    /// * RSA Keys (2048-4096 bits)
    /// * ECDSA Keys (P-256, P-384)
    /// 
    /// Thats' for both account keys and cert keys.
    /// </summary>
    public static class CryptoHelper
    {
        public static RSAParameters GenerateRsaKeys(int bits)
        {
            var rsa = RSA.Create(bits);
            return rsa.ExportParameters(true);
        }

        public static ECParameters GenerateEcKeys(int curveSize)
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


            var ec = ECDsa.Create(curve);
            return ec.ExportParameters(true);
        }

        /// <summary>
        /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static string Base64UrlEncode(string raw, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return Base64UrlEncode(encoding.GetBytes(raw));
        }

        /// <summary>
        /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static string Base64UrlEncode(byte[] raw)
        {
            string enc = Convert.ToBase64String(raw);  // Regular base64 encoder
            enc = enc.Split('=')[0];                   // Remove any trailing '='s
            enc = enc.Replace('+', '-');               // 62nd char of encoding
            enc = enc.Replace('/', '_');               // 63rd char of encoding
            return enc;
        }

        /// <summary>
        /// URL-safe Base64 decoding as prescribed in RFC 7515 Appendix C.
        /// </summary>
        public static byte[] Base64UrlDecode(string enc)
        {
            string raw = enc;
            raw = raw.Replace('-', '+');  // 62nd char of encoding
            raw = raw.Replace('_', '/');  // 63rd char of encoding
            switch (raw.Length % 4)       // Pad with trailing '='s
            {
                case 0: break;               // No pad chars in this case
                case 2: raw += "=="; break;  // Two pad chars
                case 3: raw += "="; break;   // One pad char
                default:
                    throw new System.Exception("Illegal base64url string!");
            }
            return Convert.FromBase64String(raw); // Standard base64 decoder
        }

        public static string Base64UrlDecodeToString(string enc, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            return encoding.GetString(Base64UrlDecode(enc));
        }
    }
}