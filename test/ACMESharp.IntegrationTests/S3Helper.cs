using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Logging;

namespace ACMESharp.IntegrationTests
{
    public class S3Helper
    {
        public string AwsProfileName { get; set; }

        public string AwsRegion { get; set; }

        public string BucketName { get; set; }

        public string CannedAcl
        {
            get { return S3CannedAcl?.Value; }
            set
            {
                S3CannedAcl = S3CannedACL.FindValue(value);
            }
        }
        
        public S3CannedACL S3CannedAcl
        { get; set; }

        public async Task EditFile(string filePath, string contentType, string content)
        {
#pragma warning disable 618 // "'StoredProfileCredentials' is obsolete..."
            var creds = new StoredProfileAWSCredentials("acmesharp-tests");
#pragma warning restore 618
            var reg = RegionEndpoint.GetBySystemName(AwsRegion);
            var delete = content == null;

            // We need to strip off any leading '/' in the path or
            // else it creates a path with an empty leading segment
            // if (filePath.StartsWith("/"))
            //     filePath = filePath.Substring(1);
            filePath = filePath.Trim('/');

            using (var s3 = new Amazon.S3.AmazonS3Client(creds, reg))
            {
                if (delete)
                {
                    var s3Requ = new Amazon.S3.Model.DeleteObjectRequest
                    {
                        BucketName = BucketName,
                        Key = filePath,
                    };
                    var s3Resp = await s3.DeleteObjectAsync(s3Requ);
				}
				else
                {
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                    {
                        var s3Requ = new Amazon.S3.Model.PutObjectRequest
                        {
                            BucketName = BucketName,
                            Key = filePath,
                            ContentType = contentType,
                            InputStream = ms,
                            CannedACL = S3CannedAcl,
                        };

                        var s3Resp = await s3.PutObjectAsync(s3Requ);
                    }
				}
			}
        }
    }
}