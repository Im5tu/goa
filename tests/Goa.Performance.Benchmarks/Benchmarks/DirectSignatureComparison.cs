using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.Internal.Auth;
using Amazon.SQS;
using BenchmarkDotNet.Order;
using Goa.Clients.Core.Credentials;
using Goa.Clients.Core.Http;
using Goa.Performance.Benchmarks.Infrastructure;

namespace Goa.Performance.Benchmarks.Benchmarks;

/// <summary>
/// Direct comparison benchmark between AWS SDK's signing and Goa's RequestSigner.
/// This benchmark eliminates HTTP infrastructure overhead for a pure algorithmic comparison.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[CategoriesColumn]
public class DirectSignatureComparison
{
    private string _smallPayload = null!;
    private string _mediumPayload = null!;
    private string _largePayload = null!;
    private string _xlargePayload = null!;

    // Pre-computed bytes for Goa benchmarks (avoids encoding in benchmark hot path)
    private byte[] _smallPayloadBytes = null!;
    private byte[] _mediumPayloadBytes = null!;
    private byte[] _largePayloadBytes = null!;
    private byte[] _xlargePayloadBytes = null!;

    // Goa components
    private RequestSigner _goaSigner = null!;
    private AwsCredentials _goaCredentials = null!;
    private HttpRequestMessage _goaRequestSmall = null!;
    private HttpRequestMessage _goaRequestMedium = null!;
    private HttpRequestMessage _goaRequestLarge = null!;
    private HttpRequestMessage _goaRequestXLarge = null!;

    // AWS SDK components
    private AWS4Signer _awsSigner = null!;
    private ImmutableCredentials _awsImmutableCredentials = null!;
    private AmazonSQSConfig _sqsConfig = null!;
    private IRequest _awsRequestSmall = null!;
    private IRequest _awsRequestMedium = null!;
    private IRequest _awsRequestLarge = null!;
    private IRequest _awsRequestXLarge = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test payloads (strings for AWS SDK)
        _smallPayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.Small);
        _mediumPayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.Medium);
        _largePayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.Large);
        _xlargePayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.XLarge);

        // Pre-compute bytes for Goa (simulates real-world where serialization produces bytes directly)
        _smallPayloadBytes = Encoding.UTF8.GetBytes(_smallPayload);
        _mediumPayloadBytes = Encoding.UTF8.GetBytes(_mediumPayload);
        _largePayloadBytes = Encoding.UTF8.GetBytes(_largePayload);
        _xlargePayloadBytes = Encoding.UTF8.GetBytes(_xlargePayload);

        // Setup Goa components - pre-fetch credentials to avoid async overhead in benchmark
        _goaCredentials = new AwsCredentials(
            BenchmarkConfig.AccessKey,
            BenchmarkConfig.SecretKey,
            BenchmarkConfig.SessionToken);
        var goaCredentialProvider = new StaticCredentialProvider(
            BenchmarkConfig.AccessKey,
            BenchmarkConfig.SecretKey,
            BenchmarkConfig.SessionToken);
        var goaCredentialChain = new CredentialProviderChain(new[] { goaCredentialProvider });
        _goaSigner = new RequestSigner(goaCredentialChain);

        // Prepare Goa requests (reused, headers cleared between iterations)
        _goaRequestSmall = CreateGoaRequest(_smallPayloadBytes);
        _goaRequestMedium = CreateGoaRequest(_mediumPayloadBytes);
        _goaRequestLarge = CreateGoaRequest(_largePayloadBytes);
        _goaRequestXLarge = CreateGoaRequest(_xlargePayloadBytes);

        // Setup AWS SDK components
        var awsCredentials = BenchmarkConfig.GetAwsCredentials();
        _awsImmutableCredentials = awsCredentials.GetCredentials();
        _awsSigner = new AWS4Signer();
        _sqsConfig = new AmazonSQSConfig()
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(BenchmarkConfig.Region)
        };

        // Prepare AWS requests (reused, headers cleared between iterations)
        _awsRequestSmall = CreateAwsRequest(_smallPayload);
        _awsRequestMedium = CreateAwsRequest(_mediumPayload);
        _awsRequestLarge = CreateAwsRequest(_largePayload);
        _awsRequestXLarge = CreateAwsRequest(_xlargePayload);
    }

    // Goa Benchmarks - use pre-fetched credentials to eliminate async credential lookup overhead
    [Benchmark]
    [BenchmarkCategory("Small")]
    public async Task<string> Goa_SignRequest_Small()
    {
        ClearSigningHeaders(_goaRequestSmall);
        return await _goaSigner.SignRequestAsync(_goaRequestSmall, _goaCredentials);
    }

    [Benchmark]
    [BenchmarkCategory("Medium")]
    public async Task<string> Goa_SignRequest_Medium()
    {
        ClearSigningHeaders(_goaRequestMedium);
        return await _goaSigner.SignRequestAsync(_goaRequestMedium, _goaCredentials);
    }

    [Benchmark]
    [BenchmarkCategory("Large")]
    public async Task<string> Goa_SignRequest_Large()
    {
        ClearSigningHeaders(_goaRequestLarge);
        return await _goaSigner.SignRequestAsync(_goaRequestLarge, _goaCredentials);
    }

    [Benchmark]
    [BenchmarkCategory("XLarge")]
    public async Task<string> Goa_SignRequest_XLarge()
    {
        ClearSigningHeaders(_goaRequestXLarge);
        return await _goaSigner.SignRequestAsync(_goaRequestXLarge, _goaCredentials);
    }

    // AWS SDK Benchmarks
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Small")]
    public string AwsSdk_SignRequest_Small()
    {
        ClearSigningHeaders(_awsRequestSmall);
        return SignWithAwsSdk(_awsRequestSmall);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Medium")]
    public string AwsSdk_SignRequest_Medium()
    {
        ClearSigningHeaders(_awsRequestMedium);
        return SignWithAwsSdk(_awsRequestMedium);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Large")]
    public string AwsSdk_SignRequest_Large()
    {
        ClearSigningHeaders(_awsRequestLarge);
        return SignWithAwsSdk(_awsRequestLarge);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("XLarge")]
    public string AwsSdk_SignRequest_XLarge()
    {
        ClearSigningHeaders(_awsRequestXLarge);
        return SignWithAwsSdk(_awsRequestXLarge);
    }

    private HttpRequestMessage CreateGoaRequest(byte[] payload)
    {
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-amz-json-1.1");

        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/")
        {
            Content = content
        };

        request.Options.Set(HttpOptions.Region, BenchmarkConfig.Region);
        request.Options.Set(HttpOptions.Service, "sqs");
        request.Options.Set(HttpOptions.Target, "AmazonSQS.SendMessage");
        request.Options.Set(HttpOptions.Payload, payload);

        return request;
    }

    private IRequest CreateAwsRequest(string payload)
    {
        var awsRequest = new Amazon.SQS.Model.SendMessageRequest();
        var request = new DefaultRequest(awsRequest, "SQS");
        request.HttpMethod = "POST";
        request.Endpoint = new Uri("https://sqs.us-east-1.amazonaws.com/");
        request.ResourcePath = "/";
        request.Content = Encoding.UTF8.GetBytes(payload);
        request.Headers["Content-Type"] = "application/x-amz-json-1.1";
        request.Headers["X-Amz-Target"] = "AmazonSQS.SendMessage";

        return request;
    }

    private string SignWithAwsSdk(IRequest request)
    {
        var signingResult = _awsSigner.SignRequest(request, _sqsConfig, null,
            _awsImmutableCredentials.AccessKey, _awsImmutableCredentials.SecretKey);
        return signingResult.Signature;
    }

    private static void ClearSigningHeaders(HttpRequestMessage request)
    {
        request.Headers.Remove("Authorization");
        request.Headers.Remove("X-Amz-Date");
        request.Headers.Remove("X-Amz-Security-Token");
        request.Headers.Remove("x-amz-content-sha256");
    }

    private static void ClearSigningHeaders(IRequest request)
    {
        request.Headers.Remove("Authorization");
        request.Headers.Remove("X-Amz-Date");
        request.Headers.Remove("X-Amz-Security-Token");
        request.Headers.Remove("x-amz-content-sha256");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _goaRequestSmall?.Dispose();
        _goaRequestMedium?.Dispose();
        _goaRequestLarge?.Dispose();
        _goaRequestXLarge?.Dispose();
    }
}
