using System;
using System.Text;

namespace ACMESharp.Crypto
{
    public static partial class CryptoHelper
    {

        /// <summary>
        /// Collection of convenient crypto operations working
        /// with URL-safe Base64 encoding.
        /// </summary>
        public static partial class Base64
        {

            /// <summary>
            /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
            /// </summary>
            public static string UrlEncode(string raw, Encoding? encoding = null)
            {
                encoding ??= Encoding.UTF8;
                return UrlEncode(encoding.GetBytes(raw));
            }

            /// <summary>
            /// URL-safe Base64 encoding as prescribed in RFC 7515 Appendix C.
            /// </summary>
            public static string UrlEncode(byte[] raw)
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
            public static byte[] UrlDecode(string enc)
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

            public static string UrlDecodeToString(string enc, Encoding? encoding = null)
            {
                encoding ??= Encoding.UTF8;
                return encoding.GetString(UrlDecode(enc));
            }
        }
    }
}