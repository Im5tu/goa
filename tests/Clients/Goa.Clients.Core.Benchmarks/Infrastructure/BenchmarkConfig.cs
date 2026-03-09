using Amazon.Runtime;

namespace Goa.Clients.Core.Benchmarks.Infrastructure;

public static class BenchmarkConfig
{
    public static string AccessKey => "AKIAIOSFODNN7EXAMPLE";
    public static string SecretKey => "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
    public static string SessionToken => "AQoDYXdzEJr0987654321EXAMPLE";
    public static string Region => "us-east-1";

    public static class PayloadSizes
    {
        public const int Small = 1024;
        public const int Medium = 10240;
        public const int Large = 102400;
        public const int XLarge = 1048576;
    }

    public static BasicAWSCredentials GetAwsCredentials()
    {
        return new BasicAWSCredentials(AccessKey, SecretKey);
    }

    public static SessionAWSCredentials GetAwsSessionCredentials()
    {
        return new SessionAWSCredentials(AccessKey, SecretKey, SessionToken);
    }

    public static string GeneratePayload(int sizeInBytes)
    {
        var random = new Random(42);
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringBuilder = new StringBuilder(sizeInBytes);

        for (int i = 0; i < sizeInBytes; i++)
        {
            stringBuilder.Append(chars[random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }
}
