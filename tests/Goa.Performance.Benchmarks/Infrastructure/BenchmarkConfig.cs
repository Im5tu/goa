using Amazon.Runtime;

namespace Goa.Performance.Benchmarks.Infrastructure;

public static class BenchmarkConfig
{
    public static string AccessKey => "AKIAIOSFODNN7EXAMPLE";
    public static string SecretKey => "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
    public static string SessionToken => "AQoDYXdzEJr0987654321EXAMPLE";
    public static string Region => "us-east-1";

    // LocalStack endpoints
    public static string LocalStackEndpoint => "http://localhost:4566";

    // Test data sizes for benchmarks
    public static class PayloadSizes
    {
        public const int Small = 1024;      // 1 KB
        public const int Medium = 10240;    // 10 KB
        public const int Large = 102400;    // 100 KB
        public const int XLarge = 1048576;  // 1 MB
    }

    // AWS SDK Configuration
    public static Amazon.Runtime.ClientConfig GetAwsSdkConfig(string service)
    {
        return service.ToLower() switch
        {
            "sqs" => new Amazon.SQS.AmazonSQSConfig
            {
                ServiceURL = LocalStackEndpoint,
                AuthenticationRegion = Region,
                UseHttp = true
            },
            "dynamodb" => new Amazon.DynamoDBv2.AmazonDynamoDBConfig
            {
                ServiceURL = LocalStackEndpoint,
                AuthenticationRegion = Region,
                UseHttp = true
            },
            "lambda" => new Amazon.Lambda.AmazonLambdaConfig
            {
                ServiceURL = LocalStackEndpoint,
                AuthenticationRegion = Region,
                UseHttp = true
            },
            _ => throw new ArgumentException($"Unknown service: {service}")
        };
    }

    public static BasicAWSCredentials GetAwsCredentials()
    {
        return new BasicAWSCredentials(AccessKey, SecretKey);
    }

    public static SessionAWSCredentials GetAwsSessionCredentials()
    {
        return new SessionAWSCredentials(AccessKey, SecretKey, SessionToken);
    }

    // Generate test payload of specified size
    public static string GeneratePayload(int sizeInBytes)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var stringBuilder = new StringBuilder(sizeInBytes);

        for (int i = 0; i < sizeInBytes; i++)
        {
            stringBuilder.Append(chars[random.Next(chars.Length)]);
        }

        return stringBuilder.ToString();
    }

    // Generate test data for DynamoDB
    public static Dictionary<string, object> GenerateDynamoItem(int fieldCount = 10)
    {
        var item = new Dictionary<string, object>
        {
            ["id"] = Guid.NewGuid().ToString(),
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["status"] = "ACTIVE"
        };

        for (int i = 0; i < fieldCount; i++)
        {
            item[$"field_{i}"] = $"value_{i}_{Guid.NewGuid()}";
        }

        return item;
    }
}
