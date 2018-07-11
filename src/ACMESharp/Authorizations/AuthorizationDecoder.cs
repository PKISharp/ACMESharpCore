using System;
using System.Linq;
using ACMESharp.Crypto.JOSE;
using ACMESharp.Protocol.Resources;

namespace ACMESharp.Authorizations
{
    public class AuthorizationDecoder
    {

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-8
        /// </remarks>
        public static IChallengeValidationDetails DecodeChallengeValidation(
                Authorization authz, string challengeType, JWSAlgorithm signer)
        {
            var challenge = authz.Challenges.Where(x => x.Type == challengeType)
                    .FirstOrDefault();
            if (challenge == null)
            {
                throw new InvalidOperationException(
                        $"Challenge type [{challengeType}] not found for given Authorization");
            }

            switch (challengeType)
            {
                case "dns-01":
                    return ResolveChallengeForDns01(authz, challenge, signer);
                case "http-01":
                    return ResolveChallengeForHttp01(authz, challenge, signer);
            }

            throw new NotImplementedException(
                    $"Unknown or unsupported Challenge type [{challengeType}]");
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-8.4
        /// </remarks>
        public static Dns01ChallengeValidationDetails ResolveChallengeForDns01(
                Authorization authz, Challenge challenge, JWSAlgorithm signer)
        {
            var keyAuthzDigested = JwsHelper.ComputeKeyAuthorizationDigest(
                    signer, challenge.Token);

            return new Dns01ChallengeValidationDetails
            {
                DnsRecordName = $@"{Dns01ChallengeValidationDetails.DnsRecordNamePrefix}.{
                        authz.Identifier.Value}",
                DnsRecordType = Dns01ChallengeValidationDetails.DnsRecordTypeDefault,
                DnsRecordValue = keyAuthzDigested,
            };
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// https://tools.ietf.org/html/draft-ietf-acme-acme-12#section-8.3
        /// </remarks>
        public static Http01ChallengeValidationDetails ResolveChallengeForHttp01(
                Authorization authz, Challenge challenge, JWSAlgorithm signer)
        {
            var keyAuthz = JwsHelper.ComputeKeyAuthorization(
                    signer, challenge.Token);

            return new Http01ChallengeValidationDetails
            {
                HttpResourceUrl = $@"http://{authz.Identifier.Value}/{
                        Http01ChallengeValidationDetails.HttpPathPrefix}/{
                        challenge.Token}",
                HttpResourcePath = $@"{Http01ChallengeValidationDetails.HttpPathPrefix}/{
                        challenge.Token}",
                HttpResourceContentType = Http01ChallengeValidationDetails.HttpResourceContentTypeDefault,
                HttpResourceValue = keyAuthz,
            };
        }
    }
}