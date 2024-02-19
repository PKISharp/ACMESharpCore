using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;

namespace ACMESharp.IntegrationTests
{
    public class R53Helper
    {
        public const int DefaultRecordTtl = 300;

        public string AwsProfileName = "acmesharp-tests";

        public string AwsRegion { get; set; }

        public string HostedZoneId { get; set; }

        public int DnsRecordTtl { get; set; } = DefaultRecordTtl;

        // public string DnsRecordName { get; set; }

        // public string DnsRecordType { get; set; }

        // public string DnsRecordValue { get; set; }

        /// <summary>
        /// Returns all records up to 100 at a time, starting with the
        /// one with the optional name and/or type, sorted in lexical
        /// order by name (with labels reversed) then by type.
        /// </summary>
        public async Task<ListResourceRecordSetsResponse> GetRecords(
                string startingDnsName, string startingDnsType = null)
        {
#pragma warning disable 618 // "'StoredProfileCredentials' is obsolete..."
            var crd = StoredProfileCredentials.GetProfile(AwsProfileName);
#pragma warning restore 618
            var cfg = new AmazonRoute53Config
            {
                RegionEndpoint = RegionEndpoint.USEast1,
            };

            var reg = RegionEndpoint.GetBySystemName(AwsRegion);
            using (var r53 = new Amazon.Route53.AmazonRoute53Client(crd, cfg))
            {
                var rrRequ = new Amazon.Route53.Model.ListResourceRecordSetsRequest
                {
                    HostedZoneId = HostedZoneId,
                    StartRecordName = startingDnsName,
                    StartRecordType = startingDnsType,
                };

                var rrResp = await r53.ListResourceRecordSetsAsync(rrRequ);

                return rrResp;
            }
        }

        public async Task EditTxtRecord(string dnsName, IEnumerable<string> dnsValues, bool delete = false)
        {
            var dnsValuesJoined = string.Join("\" \"", dnsValues);
            var rrSet = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = DefaultRecordTtl,
                Name = dnsName,
                Type = Amazon.Route53.RRType.TXT,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(
                            $"\"{dnsValuesJoined}\"")
                }
            };

            await EditR53Record(rrSet, delete);
        }

        public async Task EditARecord(string dnsName, string dnsValue, bool delete = false)
        {
            var rrSet =new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = DefaultRecordTtl,
                Name = dnsName,
                Type = Amazon.Route53.RRType.A,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(dnsValue)
                }
            };

            await EditR53Record(rrSet);
        }

        public async Task EditCnameRecord(string dnsName, string dnsValue, bool delete = false)
        {
            var rrSet = new Amazon.Route53.Model.ResourceRecordSet
            {
                TTL = DefaultRecordTtl,
                Name = dnsName,
                Type = Amazon.Route53.RRType.CNAME,
                ResourceRecords = new List<Amazon.Route53.Model.ResourceRecord>
                {
                    new Amazon.Route53.Model.ResourceRecord(dnsValue)
                }
            };

            await EditR53Record(rrSet);
        }

        public async Task EditR53Record(Amazon.Route53.Model.ResourceRecordSet rrSet,
                bool delete = false)
        {
#pragma warning disable 618 // "'StoredProfileCredentials' is obsolete..."
          //var creds = new BasicAWSCredentials(AwsAccessKey, AwsSecretKey);
            var creds = new StoredProfileAWSCredentials("acmesharp-tests");
#pragma warning restore 618
            var reg = RegionEndpoint.GetBySystemName(AwsRegion);
            using (var r53 = new Amazon.Route53.AmazonRoute53Client(creds, reg))
            {
                var rrRequ = new Amazon.Route53.Model.ChangeResourceRecordSetsRequest
                {
                    HostedZoneId = HostedZoneId,
                    ChangeBatch = new Amazon.Route53.Model.ChangeBatch
                    {
                        Changes = new List<Amazon.Route53.Model.Change>
                        {
                            new Amazon.Route53.Model.Change
                            {
                                Action = delete
                                    ? Amazon.Route53.ChangeAction.DELETE
                                    : Amazon.Route53.ChangeAction.UPSERT,
                                ResourceRecordSet = rrSet
                            }
                        }
                    }
                };

                var rrResp = await r53.ChangeResourceRecordSetsAsync(rrRequ);
            }
        }
    }
}