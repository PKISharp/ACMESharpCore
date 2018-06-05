using System.Runtime.InteropServices;

namespace ACMESharp.IntegrationTests.Debugging
{
    internal static class MsBuild
    {
        public static string MsBuildName { get; }

        static MsBuild()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                MsBuildName = "MSBuild.exe";
            else
                MsBuildName = "msbuild";
        }
    }
}