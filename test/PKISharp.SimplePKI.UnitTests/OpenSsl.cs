using System.Diagnostics;
using System.IO;

namespace PKISharp.SimplePKI.UnitTests
{
    public static class OpenSsl
    {
        public const string OpenSslLightPath = @"C:\Program Files\OpenSSL\bin\openssl.exe";

        public static Process Start(string arguments)
        {
            if (File.Exists(OpenSslLightPath))
            {
                return Process.Start(OpenSslLightPath, arguments);
            }
            else
            {
                return Process.Start("openssl", arguments);
            }
        }       
    }
}