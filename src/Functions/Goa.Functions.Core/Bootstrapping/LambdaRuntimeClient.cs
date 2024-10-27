using Goa.Functions.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Goa.Functions.Core.Bootstrapping;

internal sealed class LambdaRuntimeClient : ILambdaRuntimeClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonLogger _logger;
    private readonly Uri _nextInvocationUrl;
    private readonly Uri _initializationErrorUrl;
    private readonly string _invocationErrorUrlTemplate;
    private readonly string _invocationResponseUrlTemplate;

    public LambdaRuntimeClient(LogLevel logLevel, HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? CreateHttpClient();
        _logger = new JsonLogger(nameof(LambdaRuntimeClient), logLevel, LogScopeProvider.Instance, LoggingSerializationContext.Default);

        var runtimeApiBaseUrl = $"http://{Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")}/2018-06-01/runtime";
        _nextInvocationUrl = new Uri($"{runtimeApiBaseUrl}/invocation/next", UriKind.Absolute);
        _initializationErrorUrl = new Uri($"{runtimeApiBaseUrl}/init/error", UriKind.Absolute);
        _invocationErrorUrlTemplate = $"{runtimeApiBaseUrl}/invocation/{{0}}/error";
        _invocationResponseUrlTemplate = $"{runtimeApiBaseUrl}/invocation/{{0}}/response";
    }

    public async Task<Result<InvocationRequest>> GetNextInvocationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.GetNextInvocationStart();

            using var request = new HttpRequestMessage(HttpMethod.Get, _nextInvocationUrl);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result<InvocationRequest>.Failure($"Failed to get next invocation. Status Code: {response.StatusCode}");
            }

            string? requestId = null;
            string? deadlineMs = null;
            string? functionArn = null;

            if (response.Headers.TryGetValues("Lambda-Runtime-Aws-Request-Id", out var requestIdValues))
            {
                requestId = requestIdValues.FirstOrDefault() ?? string.Empty;
            }
            if (response.Headers.TryGetValues("Lambda-Runtime-Deadline-Ms", out var deadlineMsValues))
            {
                deadlineMs = deadlineMsValues.FirstOrDefault() ?? string.Empty;
            }
            if (response.Headers.TryGetValues("Lambda-Runtime-Invoked-Function-Arn", out var functionArnValues))
            {
                functionArn = functionArnValues.FirstOrDefault() ?? string.Empty;
            }

            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            if (requestId == null)
            {
                return Result<InvocationRequest>.Failure("Missing RequestId in the Lambda invocation response.");
            }

            if (string.IsNullOrWhiteSpace(functionArn))
            {
                return Result<InvocationRequest>.Failure("Missing FunctionArn in the Lambda invocation response.");
            }

            if (string.IsNullOrWhiteSpace(deadlineMs))
            {
                return Result<InvocationRequest>.Failure("Missing DeadlineMs in the Lambda invocation response.");
            }

            _logger.GetNextInvocationComplete();
            return Result<InvocationRequest>.Success(new InvocationRequest(requestId, payload, deadlineMs, functionArn));
        }
        catch (TaskCanceledException)
        {
            return Result<InvocationRequest>.Failure("No pending invocation could be found.");
        }
        catch (HttpRequestException ex)
        {
            _logger.GetNextInvocationError(ex);
            return Result<InvocationRequest>.Failure(ex.Message);
        }
    }

    public async Task<Result> ReportInvocationErrorAsync(string awsRequestId, InvocationErrorPayload errorPayload, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = string.Format(_invocationErrorUrlTemplate, awsRequestId);
            var content = new StringContent(JsonSerializer.Serialize(errorPayload, RuntimeClientSerializationContext.Default.InvocationErrorPayload), Encoding.UTF8, "application/json");

            _logger.ReportInvocationErrorStart();
            using var response = await _httpClient.PostAsync(url, content, cancellationToken);
            _logger.ReportInvocationErrorComplete((int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure($"Failed to report invocation error for RequestId {awsRequestId}. Status Code: {response.StatusCode}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ReportInvocationErrorFailed(ex);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> ReportInitializationErrorAsync(InitializationErrorPayload errorPayload, CancellationToken cancellationToken = default)
    {
        try
        {
            var content = new StringContent(JsonSerializer.Serialize(errorPayload, RuntimeClientSerializationContext.Default.InitializationErrorPayload), Encoding.UTF8, "application/json");

            _logger.ReportInitializationErrorStart();
            using var response = await _httpClient.PostAsync(_initializationErrorUrl, content, cancellationToken);
            _logger.ReportInitializationErrorComplete((int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure($"Failed to report initialization error. Status Code: {response.StatusCode}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.ReportInitializationErrorFailed(ex);
            return Result.Failure(ex.Message);
        }
    }

    public async Task<Result> SendResponseAsync(string awsRequestId, HttpContent content, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = string.Format(_invocationResponseUrlTemplate, awsRequestId);

            _logger.SendResponseStart();
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _logger.SendResponseComplete((int)response.StatusCode);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Failure($"Failed to send response for RequestId {awsRequestId}. Status Code: {response.StatusCode}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.SendResponseFailed(ex);
            return Result.Failure(ex.Message);
        }
    }

    private static HttpClient CreateHttpClient() => new(new SocketsHttpHandler
    {
        UseCookies = false,
        UseProxy = false,
        // HttpClient by default supports only ASCII characters in headers. Changing it to allow UTF8 characters.
        RequestHeaderEncodingSelector = delegate { return Encoding.UTF8; },
        ResponseHeaderEncodingSelector = delegate { return Encoding.UTF8; }
    })
    {
        Timeout = string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_LAMBDA_RUNTIME_API")) ? TimeSpan.FromSeconds(2) : TimeSpan.FromSeconds(100),
        DefaultRequestVersion = new Version(1, 1),
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact
    };
}
