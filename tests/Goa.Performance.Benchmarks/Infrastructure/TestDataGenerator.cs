using System.Text.Json;

namespace Goa.Performance.Benchmarks.Infrastructure;

public static class TestDataGenerator
{
    private static readonly Random _random = new(42); // Fixed seed for reproducible results

    public static class SqsTestData
    {
        public static string CreateMessage(int sizeBytes)
        {
            return BenchmarkConfig.GeneratePayload(sizeBytes);
        }

        public static IEnumerable<string> CreateMessages(int count, int sizeBytes)
        {
            for (int i = 0; i < count; i++)
            {
                yield return CreateMessage(sizeBytes);
            }
        }
    }

    public static class DynamoTestData
    {
        public static Dictionary<string, object> CreateItem(string? id = null, int extraFieldCount = 5)
        {
            return new Dictionary<string, object>
            {
                ["id"] = id ?? Guid.NewGuid().ToString(),
                ["name"] = $"TestItem_{_random.Next(1000, 9999)}",
                ["category"] = $"Category{_random.Next(1, 10)}",
                ["price"] = _random.Next(1, 1000),
                ["description"] = BenchmarkConfig.GeneratePayload(_random.Next(50, 200)),
                ["created_at"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ["tags"] = CreateStringArray(3, 10),
                ["metadata"] = CreateNestedObject(extraFieldCount)
            };
        }

        private static string[] CreateStringArray(int minItems, int maxItems)
        {
            var count = _random.Next(minItems, maxItems + 1);
            var array = new string[count];
            
            for (int i = 0; i < count; i++)
            {
                array[i] = $"tag_{_random.Next(1, 100)}";
            }
            
            return array;
        }

        private static Dictionary<string, object> CreateNestedObject(int fieldCount)
        {
            var obj = new Dictionary<string, object>();
            
            for (int i = 0; i < fieldCount; i++)
            {
                obj[$"field_{i}"] = _random.Next(1, 1000);
            }
            
            return obj;
        }
    }

    public static class SigningTestData
    {
        public static string CreateCanonicalRequest(string method = "POST", string path = "/", int payloadSize = 1024)
        {
            var payload = BenchmarkConfig.GeneratePayload(payloadSize);
            var headers = new Dictionary<string, string>
            {
                ["host"] = "sqs.us-east-1.amazonaws.com",
                ["x-amz-date"] = "20240821T120000Z",
                ["x-amz-content-sha256"] = ComputeSha256(payload),
                ["content-type"] = "application/x-amz-json-1.1"
            };

            var canonicalHeaders = string.Join("\n", headers.OrderBy(h => h.Key).Select(h => $"{h.Key}:{h.Value}")) + "\n";
            var signedHeaders = string.Join(";", headers.Keys.OrderBy(k => k));
            var payloadHash = ComputeSha256(payload);

            return $"{method}\n{path}\n\n{canonicalHeaders}\n{signedHeaders}\n{payloadHash}";
        }

        public static (string accessKey, string secretKey, string sessionToken, string region, string service) GetSigningInputs()
        {
            return (BenchmarkConfig.AccessKey, BenchmarkConfig.SecretKey, BenchmarkConfig.SessionToken, 
                   BenchmarkConfig.Region, "sqs");
        }

        private static string ComputeSha256(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLower();
        }
    }

    public static class LambdaTestData
    {
        public static string CreateInvokePayload(int sizeBytes = 1024)
        {
            var basePayload = new
            {
                requestId = Guid.NewGuid().ToString(),
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                data = BenchmarkConfig.GeneratePayload(sizeBytes - 100) // Account for other fields
            };

            return JsonSerializer.Serialize(basePayload);
        }

        public static IEnumerable<string> CreateInvokePayloads(int count, int sizeBytes)
        {
            for (int i = 0; i < count; i++)
            {
                yield return CreateInvokePayload(sizeBytes);
            }
        }
    }
}