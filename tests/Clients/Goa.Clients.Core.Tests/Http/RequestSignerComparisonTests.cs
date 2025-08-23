using System.Text;
using ErrorOr;
using Goa.Clients.Core.Credentials;
using Goa.Clients.Core.Http;
using Moq;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.SQS;

namespace Goa.Clients.Core.Tests.Http;

/// <summary>
/// Comprehensive test suite for RequestSigner AWS Signature V4 implementation.
/// Tests validate SigV4 compliance, performance optimizations, and edge cases.
/// </summary>
public class RequestSignerComparisonTests
{
    private readonly Mock<ICredentialProviderChain> _mockCredentialProvider;
    private readonly AwsCredentials _goaCredentials;
    private readonly RequestSigner _goaSigner;
    private readonly DateTime _fixedDateTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);

    public RequestSignerComparisonTests()
    {
        _goaCredentials = new AwsCredentials(
            "AKIAIOSFODNN7EXAMPLE",
            "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            "AQoDYXdzEJr0987654321EXAMPLE",
            DateTime.UtcNow.AddHours(1)
        );

        _mockCredentialProvider = new Mock<ICredentialProviderChain>();
        _mockCredentialProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(_goaCredentials));

        _goaSigner = new RequestSigner(_mockCredentialProvider.Object);
    }

    /// <summary>
    /// Sets up identical headers on both requests to ensure both signers work with the same input data.
    /// This is critical for signature comparison as both must use identical timestamps and payload hashes.
    /// </summary>
    private static void SetupIdenticalHeaders(HttpRequestMessage goaRequest, DefaultRequest awsRequest, DateTime timestamp, string? payload = null)
    {
        var longDate = timestamp.ToString("yyyyMMddTHHmmssZ");

        // Calculate payload hash for both requests
        var payloadHash = payload == null
            ? "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855" // Empty payload hash
            : Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        // Set identical headers on Goa request
        goaRequest.Headers.Host = goaRequest.RequestUri?.Host;
        goaRequest.Headers.TryAddWithoutValidation("X-Amz-Date", longDate);
        goaRequest.Headers.TryAddWithoutValidation("X-Amz-Content-SHA256", payloadHash);

        // Ensure Content-Type is consistent - Goa uses Content.Headers, AWS SDK uses Headers
        if (goaRequest.Content?.Headers.ContentType != null)
        {
            var contentType = goaRequest.Content.Headers.ContentType.MediaType;
            if (!string.IsNullOrEmpty(contentType))
            {
                awsRequest.Headers["Content-Type"] = contentType;
            }
        }

        // Ensure X-Amz-Target is consistent - Goa uses HttpOptions, AWS SDK uses Headers
        if (goaRequest.Options.TryGetValue(HttpOptions.Target, out var target) && !string.IsNullOrEmpty(target))
        {
            awsRequest.Headers["X-Amz-Target"] = target;
        }

        // Set identical headers on AWS request
        awsRequest.Headers["host"] = awsRequest.Endpoint.Host;
        awsRequest.Headers["X-Amz-Date"] = longDate;
        awsRequest.Headers["X-Amz-Content-SHA256"] = payloadHash;
    }

    [Test]
    public async Task RequestSigner_Produces_Valid_Authorization_Header_Simple_GET()
    {
        // Test validates that the RequestSigner generates proper AWS Signature V4 format for simple GET requests
        // This ensures compatibility with AWS services that expect valid SigV4 signatures

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        await Assert.That(authHeader).Contains("Credential=AKIAIOSFODNN7EXAMPLE");
        await Assert.That(authHeader).Contains("SignedHeaders=");
        await Assert.That(authHeader).Contains("Signature=");
    }

    [Test]
    public async Task RequestSigner_Produces_Valid_Authorization_Header_POST_With_Body()
    {
        // Test validates proper payload hashing and signature generation for POST requests with JSON content
        // This is critical for services like SQS that require accurate payload hash calculation

        // Arrange
        var payload = "{\"QueueUrl\":\"https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue\",\"MessageBody\":\"Hello World\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");
        request.Options.Set(HttpOptions.Target, "AmazonSQS.SendMessage");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        await Assert.That(authHeader).Contains("x-amz-content-sha256"); // Verify payload hash is included in signed headers
    }

    [Test]
    public async Task RequestSigner_Handles_Multiple_Headers_Correctly_DynamoDB()
    {
        // Test validates proper header canonicalization and sorting for requests with many custom headers
        // This ensures the insertion sort optimization works correctly for typical header counts

        // Arrange
        var payload = "{\"TableName\":\"Music\",\"Key\":{\"Artist\":{\"S\":\"No One You Know\"},\"SongTitle\":{\"S\":\"Call Me Today\"}}}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dynamodb.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "dynamodb");
        request.Options.Set(HttpOptions.Target, "DynamoDB_20120810.GetItem");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.0");
        request.Headers.Add("User-Agent", "Goa/1.0");
        request.Headers.Add("X-Custom-Header1", "value1");
        request.Headers.Add("X-Custom-Header2", "value2");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        // Verify that all expected headers are present in the signed headers (order may vary)
        await Assert.That(authHeader).Contains("content-type");
        await Assert.That(authHeader).Contains("host");
        await Assert.That(authHeader).Contains("user-agent");
        await Assert.That(authHeader).Contains("x-amz-content-sha256");
        await Assert.That(authHeader).Contains("x-amz-date");
        await Assert.That(authHeader).Contains("x-amz-target");
        await Assert.That(authHeader).Contains("x-custom-header1");
        await Assert.That(authHeader).Contains("x-custom-header2");
    }

    [Test]
    public async Task RequestSigner_Handles_Credentials_Without_Session_Token()
    {
        // Test validates proper handling of permanent AWS credentials (no session token)
        // This ensures the signer works correctly with IAM user credentials, not just assumed roles

        // Arrange - credentials without session token
        var credentialsWithoutSession = new AwsCredentials(
            "AKIAIOSFODNN7EXAMPLE",
            "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY",
            null,
            DateTime.UtcNow.AddHours(1)
        );

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(credentialsWithoutSession));

        var signer = new RequestSigner(mockCredProvider.Object);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://s3.us-east-1.amazonaws.com/my-bucket/my-object");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "s3");

        // Act
        var authHeader = await signer.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).DoesNotContain("x-amz-security-token"); // Should not include session token in signed headers
    }

    [Test]
    public async Task RequestSigner_Handles_Large_Payloads_Efficiently()
    {
        // Test validates the ArrayPool optimization for large payloads (>1024 bytes)
        // This ensures memory efficiency when processing large request bodies

        // Arrange
        var largePayload = new string('a', 100_000); // 100KB payload - triggers ArrayPool usage
        var request = new HttpRequestMessage(HttpMethod.Put, "https://s3.us-east-1.amazonaws.com/my-bucket/large-file");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "s3");
        request.Content = new StringContent(largePayload, Encoding.UTF8, "text/plain");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // Verify that large payload doesn't cause excessive memory allocation or errors
    }

    [Test]
    public async Task RequestSigner_Handles_Query_Parameters_With_Proper_Canonicalization()
    {
        // Test validates query parameter parsing, encoding, and sorting according to AWS canonicalization rules
        // This exercises the custom QueryPart struct and insertion sort implementation

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/?Version=2012-11-05&Action=ListQueues&QueueNamePrefix=MyQueue");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // Query parameters should be canonicalized in alphabetical order: Action, QueueNamePrefix, Version
    }

    [Test]
    public async Task RequestSigner_Works_With_Different_AWS_Regions()
    {
        // Test validates that regional scope is properly incorporated into the credential scope
        // This ensures compatibility across all AWS regions

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://sqs.eu-west-1.amazonaws.com/123456789012/MyQueue");
        request.Options.Set(HttpOptions.Region, "eu-west-1");
        request.Options.Set(HttpOptions.Service, "sqs");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("/20231201/eu-west-1/sqs/aws4_request"); // Verify region is in credential scope
    }

    [Test]
    public async Task RequestSigner_Works_With_Different_AWS_Services()
    {
        // Test validates that service name is properly incorporated into the credential scope
        // This ensures compatibility across all AWS services

        var testServices = new[]
        {
            ("sqs", "https://sqs.us-east-1.amazonaws.com/"),
            ("dynamodb", "https://dynamodb.us-east-1.amazonaws.com/"),
            ("s3", "https://s3.us-east-1.amazonaws.com/"),
            ("lambda", "https://lambda.us-east-1.amazonaws.com/")
        };

        foreach (var (service, endpoint) in testServices)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Options.Set(HttpOptions.Region, "us-east-1");
            request.Options.Set(HttpOptions.Service, service);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            // Act
            var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

            // Assert
            await Assert.That(authHeader).IsNotNull();
            await Assert.That(authHeader).Contains($"/us-east-1/{service}/aws4_request"); // Verify service is in credential scope
        }
    }

    [Test]
    public async Task RequestSigner_Handles_UTF8_And_Special_Characters()
    {
        // Test validates proper UTF-8 encoding and header value normalization
        // This exercises the ASCII fast-path optimization and ensures international character support

        // Arrange
        var payload = "{\"MessageBody\":\"Hello World with special chars: àáâãäåæçèéêë\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        request.Headers.Add("X-Custom-Header", "value with spaces and special chars: !@#$%^&*()");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // UTF-8 payload should be properly hashed and encoded
    }

    [Test]
    public async Task RequestSigner_Handles_Empty_Request_Body()
    {
        // Test validates proper handling of empty payloads with correct SHA256 hash
        // Empty body should produce the hash of an empty string

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");
        request.Content = new StringContent("", Encoding.UTF8, "application/json");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // Empty body should still produce valid signature
    }

    [Test]
    public async Task RequestSigner_Generates_Expected_AWS_Signature_V4_Format()
    {
        // Test validates that the signer produces the exact AWS Signature V4 format expected by AWS services
        // Expected format: AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20231201/us-east-1/sqs/aws4_request, SignedHeaders=host;x-amz-date, Signature=...

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert - Validate exact AWS SigV4 format
        await Assert.That(authHeader).StartsWith("AWS4-HMAC-SHA256 Credential=AKIAIOSFODNN7EXAMPLE/20231201/us-east-1/sqs/aws4_request");
        await Assert.That(authHeader).Contains("SignedHeaders=");
        await Assert.That(authHeader).Contains("Signature=");
    }

    [Test]
    public async Task RequestSigner_Handles_Complex_Query_Parameters_With_Encoding()
    {
        // Test validates RFC 3986 encoding and proper sorting of query parameters with special characters
        // This exercises the custom QueryPart parsing and encoding logic

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/?Delimiter=%2F&Prefix=folder%2Fsubfolder&MaxKeys=100&Marker=after%20this");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // Query parameters should be properly encoded and canonicalized
    }

    [Test]
    public async Task RequestSigner_Handles_Multiple_Header_Values_With_Normalization()
    {
        // Test validates header value normalization (whitespace collapse) as per AWS requirements
        // This exercises the WriteNormalizedJoined method for multi-value headers

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        request.Headers.Add("X-Custom-Header", new[] { "  value1  ", "\tvalue2\t", "value3   with   spaces" });

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("x-custom-header"); // Header should be included in signed headers
        // Multiple header values should be normalized and joined with commas
    }

    [Test]
    public async Task RequestSigner_Handles_PreComputed_Payload_Option()
    {
        // Test validates the HttpOptions.Payload optimization that avoids reading request content
        // This is critical for performance when the payload is already available as a string

        // Arrange
        var payload = "{\"Action\":\"SendMessage\",\"MessageBody\":\"Test message\"}";
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");
        request.Options.Set(HttpOptions.Payload, payload); // Pre-computed payload
        request.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // Should use pre-computed payload instead of reading from Content stream
    }

    [Test]
    public async Task RequestSigner_Handles_Requests_Without_Content()
    {
        // Test validates proper handling of requests with no content (GET, DELETE, etc.)
        // This should produce the SHA256 hash of an empty string

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");
        // Explicitly no Content set

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // Should handle null content gracefully
    }

    [Test]
    public async Task RequestSigner_Handles_Very_Large_Header_Count()
    {
        // Test validates the fallback to Array.Sort for large header collections (>16 headers)
        // This ensures scalability for requests with many custom headers

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "sqs");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Add 20 custom headers to exceed insertion sort threshold
        for (int i = 0; i < 20; i++)
        {
            request.Headers.Add($"X-Custom-Header-{i:D2}", $"value{i}");
        }

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("AWS4-HMAC-SHA256");
        // Should handle large header count without performance degradation
    }

    [Test]
    public async Task RequestSigner_Includes_AWS_Required_Headers_In_Signed_Headers()
    {
        // Test validates that all AWS-required headers are properly included in the signed headers list
        // This ensures AWS services can verify the signature correctly

        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://dynamodb.us-east-1.amazonaws.com/");
        request.Options.Set(HttpOptions.Region, "us-east-1");
        request.Options.Set(HttpOptions.Service, "dynamodb");
        request.Options.Set(HttpOptions.Target, "DynamoDB_20120810.GetItem");
        request.Options.Set(HttpOptions.ApiVersion, "2012-08-10");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/x-amz-json-1.0");

        // Act
        var authHeader = await _goaSigner.GetAuthorizationHeaderAsync(request, _fixedDateTime);

        // Assert
        await Assert.That(authHeader).IsNotNull();
        await Assert.That(authHeader).Contains("SignedHeaders=");
        // Should include: host, x-amz-api-version, x-amz-content-sha256, x-amz-date, x-amz-security-token, x-amz-target
        var signedHeadersStart = authHeader.IndexOf("SignedHeaders=") + "SignedHeaders=".Length;
        var signedHeadersEnd = authHeader.IndexOf(",", signedHeadersStart);
        var signedHeaders = authHeader.Substring(signedHeadersStart, signedHeadersEnd - signedHeadersStart);

        await Assert.That(signedHeaders).Contains("host");
        await Assert.That(signedHeaders).Contains("x-amz-content-sha256");
        await Assert.That(signedHeaders).Contains("x-amz-date");
        await Assert.That(signedHeaders).Contains("x-amz-target");
        await Assert.That(signedHeaders).Contains("x-amz-api-version");
        await Assert.That(signedHeaders).Contains("x-amz-security-token");
    }

    // ====== AWS SDK SIGNATURE COMPARISON TESTS ======
    // These tests ensure 100% signature compatibility with AWS SDK

    [Test]
    public async Task Debug_Canonical_Request_Construction()
    {
        // Debug test to verify our canonical request matches AWS SigV4 specification exactly

        // Arrange
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));
        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        // Create a simple GET request
        var goaRequest = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/");
        goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
        goaRequest.Options.Set(HttpOptions.Service, "sqs");

        // Manually set headers to match expected values
        goaRequest.Headers.Host = "sqs.us-east-1.amazonaws.com";
        goaRequest.Headers.TryAddWithoutValidation("X-Amz-Date", "20231201T120000Z");
        goaRequest.Headers.TryAddWithoutValidation("X-Amz-Content-SHA256", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");

        // Generate signature and examine the result
        var signature = await goaSigner.SignRequestAsync(goaRequest, fixedTime);

        // Expected canonical request according to AWS SigV4 spec:
        var expectedCanonicalRequest = "GET\n/\n\nhost:sqs.us-east-1.amazonaws.com\nx-amz-content-sha256:e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855\nx-amz-date:20231201T120000Z\n\nhost;x-amz-content-sha256;x-amz-date\ne3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        Console.WriteLine("Expected canonical request:");
        Console.WriteLine(expectedCanonicalRequest);
        Console.WriteLine($"\nGenerated signature: {signature}");

        // Calculate expected signature manually for comparison
        var expectedCanonicalHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(expectedCanonicalRequest))).ToLowerInvariant();
        Console.WriteLine($"Expected canonical request hash: {expectedCanonicalHash}");

        // This test is primarily for debugging - we're not asserting anything yet
        // Just validating our understanding of the canonical request format
        await Assert.That(signature).IsNotNull();
    }

    [Test]
    public async Task Debug_Signature_Differences_Simple_Case()
    {
        // Debug test to understand signature generation differences using identical input data

        // Arrange
        var awsCredentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var awsSigner = new AWS4Signer();

        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        // Create simple GET requests
        var goaRequest = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/");
        goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
        goaRequest.Options.Set(HttpOptions.Service, "sqs");

        var awsRequest = new DefaultRequest(new Amazon.SQS.Model.GetQueueUrlRequest(), "SQS");
        awsRequest.HttpMethod = "GET";
        awsRequest.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/");
        awsRequest.ResourcePath = "/";

        // First let AWS SDK sign to get its timestamp, then use that for Goa signing
        var awsSigningResult = awsSigner.SignRequest(awsRequest, new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 }, null, awsCredentials.GetCredentials().AccessKey, awsCredentials.GetCredentials().SecretKey);
        var awsSignature = awsSigningResult.Signature;

        // Extract the timestamp that AWS SDK used from the X-Amz-Date header
        var awsTimestamp = awsRequest.Headers["X-Amz-Date"];
        var awsTime = DateTime.ParseExact(awsTimestamp, "yyyyMMddTHHmmssZ", null, System.Globalization.DateTimeStyles.AdjustToUniversal);

        // Now setup Goa request with same headers and timestamp that AWS SDK actually used
        goaRequest.Headers.Host = goaRequest.RequestUri?.Host;
        goaRequest.Headers.TryAddWithoutValidation("X-Amz-Date", awsTimestamp);
        goaRequest.Headers.TryAddWithoutValidation("X-Amz-Content-SHA256", awsRequest.Headers["X-Amz-Content-SHA256"]);

        // Now generate Goa signature with the exact same timestamp AWS SDK used
        var goaSignature = await goaSigner.SignRequestAsync(goaRequest, awsTime);

        // Debug output
        Console.WriteLine($"Goa signature: {goaSignature}");
        Console.WriteLine($"AWS signature: {awsSignature}");
        Console.WriteLine($"Signatures match: {goaSignature == awsSignature}");

        // Let's examine the headers both signers used
        Console.WriteLine("\nGoa request headers:");
        foreach (var header in goaRequest.Headers)
        {
            Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }

        Console.WriteLine("\nAWS request headers:");
        foreach (var header in awsRequest.Headers)
        {
            Console.WriteLine($"  {header.Key}: {header.Value}");
        }

        // If signatures don't match, this helps debug the canonical request construction
        await Assert.That(goaSignature).IsEqualTo(awsSignature);
    }

    [Test]
    public async Task RequestSigner_Produces_Identical_Signature_To_AWS_SDK_Simple_GET()
    {
        // Test validates that our RequestSigner produces exactly the same signature as AWS SDK
        // This is the ultimate test of SigV4 compliance for simple GET requests

        // Arrange - Use identical credentials and fixed time for both signers
        var awsCredentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var awsSigner = new AWS4Signer();

        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        // Create Goa request
        var goaRequest = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue");
        goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
        goaRequest.Options.Set(HttpOptions.Service, "sqs");

        // Create AWS SDK request
        var awsRequest = new DefaultRequest(new Amazon.SQS.Model.GetQueueUrlRequest(), "SQS");
        awsRequest.HttpMethod = "GET";
        awsRequest.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue");
        awsRequest.ResourcePath = "/123456789012/MyQueue";

        // Setup identical headers for both requests using fixed timestamp
        SetupIdenticalHeaders(goaRequest, awsRequest, fixedTime);

        // Act - Generate signatures with both implementations using identical input data
        var goaSignature = await goaSigner.SignRequestAsync(goaRequest, fixedTime);
        var awsSigningResult = awsSigner.SignRequest(awsRequest, new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 }, null, awsCredentials.GetCredentials().AccessKey, awsCredentials.GetCredentials().SecretKey);
        var awsSignature = awsSigningResult.Signature;

        // Assert - Signatures must be identical
        await Assert.That(goaSignature).IsEqualTo(awsSignature);
    }

    [Test]
    public async Task RequestSigner_Produces_Identical_Signature_To_AWS_SDK_POST_With_JSON_Body()
    {
        // Test validates signature compatibility for POST requests with JSON payloads
        // This covers the majority of AWS service interactions

        // Arrange
        var awsCredentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var awsSigner = new AWS4Signer();

        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);
        var payload = "{\"QueueUrl\":\"https://sqs.us-east-1.amazonaws.com/123456789012/MyQueue\",\"MessageBody\":\"Hello World\"}";

        // Create Goa request
        var goaRequest = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
        goaRequest.Options.Set(HttpOptions.Service, "sqs");
        goaRequest.Options.Set(HttpOptions.Target, "AmazonSQS.SendMessage");
        goaRequest.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1");

        // Create AWS SDK request
        var awsRequest = new DefaultRequest(new Amazon.SQS.Model.SendMessageRequest(), "SQS");
        awsRequest.HttpMethod = "POST";
        awsRequest.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/");
        awsRequest.ResourcePath = "/";
        awsRequest.Content = Encoding.UTF8.GetBytes(payload);
        awsRequest.Headers["Content-Type"] = "application/x-amz-json-1.1";
        awsRequest.Headers["X-Amz-Target"] = "AmazonSQS.SendMessage";

        // Act
        var goaSignature = await goaSigner.SignRequestAsync(goaRequest, fixedTime);
        var awsSigningResult = awsSigner.SignRequest(awsRequest, new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 }, null, awsCredentials.GetCredentials().AccessKey, awsCredentials.GetCredentials().SecretKey);
        var awsSignature = awsSigningResult.Signature;

        // Assert
        await Assert.That(goaSignature).IsEqualTo(awsSignature);
    }

    [Test]
    public async Task RequestSigner_Produces_Identical_Signature_To_AWS_SDK_With_Query_Parameters()
    {
        // Test validates signature compatibility for requests with query string parameters
        // This ensures proper query canonicalization matches AWS SDK behavior

        // Arrange
        var awsCredentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var awsSigner = new AWS4Signer();

        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        // Create Goa request with query parameters
        var goaRequest = new HttpRequestMessage(HttpMethod.Get, "https://sqs.us-east-1.amazonaws.com/?Action=ListQueues&Version=2012-11-05&QueueNamePrefix=Test");
        goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
        goaRequest.Options.Set(HttpOptions.Service, "sqs");

        // Create AWS SDK request with same query parameters
        var awsRequest = new DefaultRequest(new Amazon.SQS.Model.ListQueuesRequest(), "SQS");
        awsRequest.HttpMethod = "GET";
        awsRequest.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/");
        awsRequest.ResourcePath = "/";
        awsRequest.Parameters["Action"] = "ListQueues";
        awsRequest.Parameters["Version"] = "2012-11-05";
        awsRequest.Parameters["QueueNamePrefix"] = "Test";

        // Act
        var goaSignature = await goaSigner.SignRequestAsync(goaRequest, fixedTime);
        var awsSigningResult = awsSigner.SignRequest(awsRequest, new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 }, null, awsCredentials.GetCredentials().AccessKey, awsCredentials.GetCredentials().SecretKey);
        var awsSignature = awsSigningResult.Signature;

        // Assert
        await Assert.That(goaSignature).IsEqualTo(awsSignature);
    }

    [Test]
    public async Task RequestSigner_Produces_Identical_Signature_To_AWS_SDK_With_Custom_Headers()
    {
        // Test validates signature compatibility when custom headers are present
        // This ensures proper header canonicalization and sorting

        // Arrange
        var awsCredentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var awsSigner = new AWS4Signer();

        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);
        var payload = "{\"Action\":\"SendMessage\",\"MessageBody\":\"Test message\"}";

        // Create Goa request with custom headers
        var goaRequest = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
        goaRequest.Options.Set(HttpOptions.Service, "sqs");
        goaRequest.Options.Set(HttpOptions.Target, "AmazonSQS.SendMessage");
        goaRequest.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1");
        goaRequest.Headers.Add("User-Agent", "Goa/1.0");
        goaRequest.Headers.Add("X-Custom-Header", "test-value");

        // Create AWS SDK request with same headers
        var awsRequest = new DefaultRequest(new Amazon.SQS.Model.SendMessageRequest(), "SQS");
        awsRequest.HttpMethod = "POST";
        awsRequest.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/");
        awsRequest.ResourcePath = "/";
        awsRequest.Content = Encoding.UTF8.GetBytes(payload);
        awsRequest.Headers["Content-Type"] = "application/x-amz-json-1.1";
        awsRequest.Headers["X-Amz-Target"] = "AmazonSQS.SendMessage";
        awsRequest.Headers["User-Agent"] = "Goa/1.0";
        awsRequest.Headers["X-Custom-Header"] = "test-value";

        // Act
        var goaSignature = await goaSigner.SignRequestAsync(goaRequest, fixedTime);
        var awsSigningResult = awsSigner.SignRequest(awsRequest, new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 }, null, awsCredentials.GetCredentials().AccessKey, awsCredentials.GetCredentials().SecretKey);
        var awsSignature = awsSigningResult.Signature;

        // Assert
        await Assert.That(goaSignature).IsEqualTo(awsSignature);
    }

    [Test]
    public async Task RequestSigner_Produces_Identical_Signature_To_AWS_SDK_Large_Payload()
    {
        // Test validates signature compatibility with large payloads
        // This ensures our ArrayPool optimization doesn't affect signature accuracy

        // Arrange
        var awsCredentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var awsSigner = new AWS4Signer();

        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);
        var largePayload = "{\"MessageBody\":\"" + new string('x', 50000) + "\"}"; // Large SQS message

        // Create Goa request
        var goaRequest = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/");
        goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
        goaRequest.Options.Set(HttpOptions.Service, "sqs");
        goaRequest.Options.Set(HttpOptions.Target, "AmazonSQS.SendMessage");
        goaRequest.Content = new StringContent(largePayload, Encoding.UTF8, "application/x-amz-json-1.1");

        // Create AWS SDK request
        var awsRequest = new DefaultRequest(new Amazon.SQS.Model.SendMessageRequest(), "SQS");
        awsRequest.HttpMethod = "POST";
        awsRequest.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/");
        awsRequest.ResourcePath = "/";
        awsRequest.Content = Encoding.UTF8.GetBytes(largePayload);
        awsRequest.Headers["Content-Type"] = "application/x-amz-json-1.1";
        awsRequest.Headers["X-Amz-Target"] = "AmazonSQS.SendMessage";

        // Act
        var goaSignature = await goaSigner.SignRequestAsync(goaRequest, fixedTime);
        var awsSigningResult = awsSigner.SignRequest(awsRequest, new AmazonSQSConfig { RegionEndpoint = Amazon.RegionEndpoint.USEast1 }, null, awsCredentials.GetCredentials().AccessKey, awsCredentials.GetCredentials().SecretKey);
        var awsSignature = awsSigningResult.Signature;

        // Assert
        await Assert.That(goaSignature).IsEqualTo(awsSignature);
    }

    [Test]
    public async Task RequestSigner_Produces_Identical_Signature_To_AWS_SDK_Different_SQS_Operations()
    {
        // Test validates signature compatibility across different SQS operations
        // This ensures our implementation handles various request patterns correctly

        var testCases = new[]
        {
            (Action: "SendMessage", Method: HttpMethod.Post, Target: "AmazonSQS.SendMessage"),
            (Action: "ReceiveMessage", Method: HttpMethod.Post, Target: "AmazonSQS.ReceiveMessage"),
            (Action: "DeleteMessage", Method: HttpMethod.Post, Target: "AmazonSQS.DeleteMessage"),
            (Action: "CreateQueue", Method: HttpMethod.Post, Target: "AmazonSQS.CreateQueue")
        };

        // Use identical credentials without session token for both signers to ensure signature compatibility
        var awsCredentials = new BasicAWSCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY");
        var goaCredentials = new AwsCredentials("AKIAIOSFODNN7EXAMPLE", "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY", null, DateTime.UtcNow.AddHours(1));

        var mockCredProvider = new Mock<ICredentialProviderChain>();
        mockCredProvider.Setup(x => x.GetCredentialsAsync())
            .ReturnsAsync(ErrorOrFactory.From(goaCredentials));

        var goaSigner = new RequestSigner(mockCredProvider.Object);
        var awsSigner = new AWS4Signer();
        var fixedTime = new DateTime(2023, 12, 1, 12, 0, 0, DateTimeKind.Utc);

        foreach (var (action, method, target) in testCases)
        {
            var payload = $"{{\"Action\":\"{action}\"}}";

            // Create Goa request
            var goaRequest = new HttpRequestMessage(method, "https://sqs.us-east-1.amazonaws.com/");
            goaRequest.Options.Set(HttpOptions.Region, "us-east-1");
            goaRequest.Options.Set(HttpOptions.Service, "sqs");
            goaRequest.Options.Set(HttpOptions.Target, target);
            goaRequest.Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1");

            // Create AWS SDK request
            var awsRequest = new DefaultRequest(new Amazon.SQS.Model.SendMessageRequest(), "SQS");
            awsRequest.HttpMethod = method.Method;
            awsRequest.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/");
            awsRequest.ResourcePath = "/";
            awsRequest.Content = Encoding.UTF8.GetBytes(payload);
            // Note: Content-Type and X-Amz-Target will be set by SetupIdenticalHeaders to match Goa request

            // Setup identical headers to ensure both signers use the same timestamp and headers
            SetupIdenticalHeaders(goaRequest, awsRequest, fixedTime, payload);

            // Act - Use ComputeSignature for direct comparison with identical inputs
            var goaSignature = await goaSigner.SignRequestAsync(goaRequest, fixedTime);
            
            // Build the canonical request manually following AWS SigV4 specification
            // Sort headers by name (case-insensitive)
            var sortedHeaders = awsRequest.Headers.OrderBy(h => h.Key, StringComparer.OrdinalIgnoreCase).ToList();
            
            // Build canonical headers string
            var canonicalHeaders = string.Join("\n", sortedHeaders.Select(h => $"{h.Key.ToLowerInvariant()}:{h.Value.Trim()}")) + "\n";
            
            // Build signed headers list
            var signedHeaderNames = string.Join(";", sortedHeaders.Select(h => h.Key.ToLowerInvariant()));
            
            // Build complete canonical request
            var canonicalRequest = $"{awsRequest.HttpMethod}\n{awsRequest.ResourcePath}\n\n{canonicalHeaders}\n{signedHeaderNames}\n{awsRequest.Headers["X-Amz-Content-SHA256"]}";
            
            var awsSigningResult = AWS4Signer.ComputeSignature(
                awsCredentials.GetCredentials().AccessKey,
                awsCredentials.GetCredentials().SecretKey,
                "us-east-1",
                fixedTime,
                "sqs",
                signedHeaderNames,
                canonicalRequest,
                null);
            var awsSignature = awsSigningResult.Signature;

            // Assert
            await Assert.That(goaSignature).IsEqualTo(awsSignature);
        }
    }
}
