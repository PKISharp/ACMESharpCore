using System.IO;
using Newtonsoft.Json;

namespace ACMESharp.IntegrationTests
{
    public class AwsFixture
    {
        public AwsFixture()
        {
            var thisAsmLocation = Path.GetDirectoryName(typeof(AwsFixture).Assembly.Location);
            var jsonPathBase = Path.Combine(thisAsmLocation, @"config/_IGNORE/");

            R53 = JsonConvert.DeserializeObject<R53Helper>(
                    File.ReadAllText(jsonPathBase + "R53Helper.json"));
            S3 = JsonConvert.DeserializeObject<S3Helper>(
                    File.ReadAllText(jsonPathBase + "S3Helper.json"));

            // For testing this makes it easier to repeat tests
            // that use the same DNS names and need to be refreshed
            R53.DnsRecordTtl = 60;
        }

        public R53Helper R53 { get; }

        public S3Helper S3 { get; }
    }
}