using System.Diagnostics;
using System.IO;

namespace PKISharp.SimplePKI.UnitTests
{
    public static class OpenSsl
    {
        public const string OpenSslLightPath = @"C:\Program Files\OpenSSL\bin\openssl.exe";

        public static Process Start(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.Arguments = arguments;
            if (File.Exists(OpenSslLightPath))
            {
                psi.EnvironmentVariables["OPENSSL_MODULES"] = "C:\\Program Files\\OpenSSL\\bin";
                psi.FileName = OpenSslLightPath;

            }
            else
            {
                psi.FileName = "openssl";
            }

            return Process.Start(psi);

        }
    }
}