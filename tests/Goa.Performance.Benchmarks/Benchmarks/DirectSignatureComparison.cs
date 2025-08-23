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

    // Goa components
    private RequestSigner _goaSigner = null!;
    private HttpRequestMessage _goaRequestSmall = null!;
    private HttpRequestMessage _goaRequestMedium = null!;
    private HttpRequestMessage _goaRequestLarge = null!;
    private HttpRequestMessage _goaRequestXLarge = null!;

    // AWS SDK components
    private AWS4Signer _awsSigner = null!;
    private BasicAWSCredentials _awsCredentials = null!;
    private AmazonSQSConfig _sqsConfig = null!;
    private IRequest _awsRequestSmall = null!;
    private IRequest _awsRequestMedium = null!;
    private IRequest _awsRequestLarge = null!;
    private IRequest _awsRequestXLarge = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Generate test payloads
        _smallPayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.Small);
        _mediumPayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.Medium);
        _largePayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.Large);
        _xlargePayload = BenchmarkConfig.GeneratePayload(BenchmarkConfig.PayloadSizes.XLarge);

        // Setup Goa components
        var goaCredentials = new StaticCredentialProvider(
            BenchmarkConfig.AccessKey,
            BenchmarkConfig.SecretKey,
            BenchmarkConfig.SessionToken);
        var goaCredentialChain = new CredentialProviderChain(new[] { goaCredentials });
        _goaSigner = new RequestSigner(goaCredentialChain);

        // Prepare Goa requests
        _goaRequestSmall = CreateGoaRequest(_smallPayload);
        _goaRequestMedium = CreateGoaRequest(_mediumPayload);
        _goaRequestLarge = CreateGoaRequest(_largePayload);
        _goaRequestXLarge = CreateGoaRequest(_xlargePayload);

        // Setup AWS SDK components
        _awsCredentials = BenchmarkConfig.GetAwsCredentials();
        _awsSigner = new AWS4Signer();
        _sqsConfig = new AmazonSQSConfig()
        {
            RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(BenchmarkConfig.Region)
        };

        // Prepare AWS requests
        _awsRequestSmall = CreateAwsRequest(_smallPayload);
        _awsRequestMedium = CreateAwsRequest(_mediumPayload);
        _awsRequestLarge = CreateAwsRequest(_largePayload);
        _awsRequestXLarge = CreateAwsRequest(_xlargePayload);
    }

    // Goa Benchmarks
    [Benchmark]
    [BenchmarkCategory("Small")]
    public async ValueTask<string> Goa_SignRequest_Small()
    {
        return await _goaSigner.SignRequestAsync(_goaRequestSmall);
    }

    [Benchmark]
    [BenchmarkCategory("Medium")]
    public async ValueTask<string> Goa_SignRequest_Medium()
    {
        return await _goaSigner.SignRequestAsync(_goaRequestMedium);
    }

    [Benchmark]
    [BenchmarkCategory("Large")]
    public async ValueTask<string> Goa_SignRequest_Large()
    {
        return await _goaSigner.SignRequestAsync(_goaRequestLarge);
    }

    [Benchmark]
    [BenchmarkCategory("XLarge")]
    public async ValueTask<string> Goa_SignRequest_XLarge()
    {
        return await _goaSigner.SignRequestAsync(_goaRequestXLarge);
    }

    // AWS SDK Benchmarks
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Small")]
    public async Task<string> AwsSdk_SignRequest_Small()
    {
        return await SignWithAwsSdk(_awsRequestSmall);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Medium")]
    public async Task<string> AwsSdk_SignRequest_Medium()
    {
        return await SignWithAwsSdk(_awsRequestMedium);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Large")]
    public async Task<string> AwsSdk_SignRequest_Large()
    {
        return await SignWithAwsSdk(_awsRequestLarge);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("XLarge")]
    public async Task<string> AwsSdk_SignRequest_XLarge()
    {
        return await SignWithAwsSdk(_awsRequestXLarge);
    }

    private HttpRequestMessage CreateGoaRequest(string payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://sqs.us-east-1.amazonaws.com/")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/x-amz-json-1.1")
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

    private async Task<string> SignWithAwsSdk(IRequest request)
    {
        var credentials = await _awsCredentials.GetCredentialsAsync();

        // Use the actual AWS4Signer to sign the request
        var signingResult = _awsSigner.SignRequest(request, _sqsConfig, null, credentials.AccessKey, credentials.SecretKey);

        return signingResult.Signature;
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
