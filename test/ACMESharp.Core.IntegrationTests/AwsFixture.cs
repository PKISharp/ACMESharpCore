using System.IO;
using Newtonsoft.Json;

namespace ACMESharp.IntegrationTests
{
    public class AwsFixture
    {
        public AwsFixture()
        {
            var jsonPath = @"C:\local\prj\bek\ACMESharp\ACMESharpCore\_IGNORE\R53Helper.json";
            var json = File.ReadAllText(jsonPath);
            R53 = JsonConvert.DeserializeObject<R53Helper>(json);

            // For testing this makes it easier to repeat tests
            // that use the same DNS names and need to be refreshed
            R53.DnsRecordTtl = 60;
        }

        public R53Helper R53 { get; }
    }
}