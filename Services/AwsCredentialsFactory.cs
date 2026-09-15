using Amazon;
using Amazon.Runtime;

namespace JobCareerPlatform.Services
{
    public static class AwsCredentialsFactory
    {
        public static RegionEndpoint GetRegion(IConfiguration configuration)
        {
            string? region = configuration["AWS:Region"];

            return string.IsNullOrWhiteSpace(region)
                ? RegionEndpoint.APSoutheast2
                : RegionEndpoint.GetBySystemName(region);
        }

        public static AWSCredentials Create(IConfiguration configuration)
        {
            string accessKey = configuration["AWS:AccessKey"] ?? string.Empty;
            string secretKey = configuration["AWS:SecretKey"] ?? string.Empty;
            string sessionToken = configuration["AWS:SessionToken"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                return FallbackCredentialsFactory.GetCredentials();
            }

            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                return new BasicAWSCredentials(accessKey, secretKey);
            }

            return new SessionAWSCredentials(accessKey, secretKey, sessionToken);
        }
    }
}
