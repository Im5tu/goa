using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibility;
using Goa.Clients.Sqs.Operations.ChangeMessageVisibilityBatch;
using Goa.Clients.Sqs.Operations.DeleteMessage;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Operations.SendMessage;
using Goa.Clients.Sqs.Operations.SendMessageBatch;
using Goa.Clients.Sqs.Serialization;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sqs;

internal sealed class SqsServiceClient : JsonAwsServiceClient<SqsServiceClientConfiguration>, ISqsClient
{
    /// <summary>
    /// The maximum visibility timeout SQS accepts, in seconds (12 hours).
    /// </summary>
    internal const int MaxVisibilityTimeoutSeconds = 43200;

    /// <summary>
    /// The maximum long-poll wait SQS accepts, in seconds.
    /// </summary>
    internal const int MaxWaitTimeSeconds = 20;

    public SqsServiceClient(
        IHttpClientFactory httpClientFactory,
        SqsServiceClientConfiguration configuration,
        ILogger<SqsServiceClient> logger)
        : base(httpClientFactory, logger, configuration)
    {
    }

    protected override System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> ResolveJsonTypeInfo<TValue>()
    {
        return SqsJsonContext.Default.GetTypeInfo(typeof(TValue))
            as System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>
            ?? throw new InvalidOperationException($"Cannot find type {typeof(TValue).Name} in serialization context");
    }

    public async Task<ErrorOr<SendMessageResponse>> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.QueueUrl))
            return Error.Validation("SendMessageRequest.QueueUrl", "Queue URL is required.");

        if (string.IsNullOrWhiteSpace(request.MessageBody))
            return Error.Validation("SendMessageRequest.MessageBody", "Message body is required.");

        try
        {
            var response = await SendWithTimeoutAsync<SendMessageRequest, SendMessageResponse>(
                request,
                "AmazonSQS.SendMessage",
                Configuration.HttpTimeout,
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled("SendMessage");
        }
        catch (Exception ex)
        {
            Logger.SendMessageFailed(ex, request.QueueUrl);
            return Error.Failure("SQS.SendMessage.Failed", $"Failed to send message to SQS queue {request.QueueUrl}");
        }
    }

    public async Task<ErrorOr<SendMessageBatchResponse>> SendMessageBatchAsync(SendMessageBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.QueueUrl))
            return Error.Validation("SendMessageBatchRequest.QueueUrl", "Queue URL is required.");

        if (request.Entries is null || request.Entries.Count == 0)
            return Error.Validation("SendMessageBatchRequest.Entries", "At least one entry is required.");

        if (request.Entries.Count > 10)
            return Error.Validation("SendMessageBatchRequest.Entries", "Maximum 10 entries allowed per batch.");

        var ids = new HashSet<string>();
        foreach (var entry in request.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
                return Error.Validation("SendMessageBatchRequestEntry.Id", "Entry ID is required for all entries.");
            if (!ids.Add(entry.Id))
                return Error.Validation("SendMessageBatchRequestEntry.Id", $"Duplicate entry ID: {entry.Id}");
        }

        try
        {
            var response = await SendWithTimeoutAsync<SendMessageBatchRequest, SendMessageBatchResponse>(
                request,
                "AmazonSQS.SendMessageBatch",
                Configuration.HttpTimeout,
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled("SendMessageBatch");
        }
        catch (Exception ex)
        {
            Logger.SendMessageBatchFailed(ex, request.QueueUrl);
            return Error.Failure("SQS.SendMessageBatch.Failed", $"Failed to send message batch to SQS queue {request.QueueUrl}");
        }
    }

    public async Task<ErrorOr<ReceiveMessageResponse>> ReceiveMessageAsync(ReceiveMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.QueueUrl))
            return Error.Validation("ReceiveMessageRequest.QueueUrl", "Queue URL is required.");

        if (request.MaxNumberOfMessages is < 1 or > 10)
            return Error.Validation("ReceiveMessageRequest.MaxNumberOfMessages", "MaxNumberOfMessages must be between 1 and 10.");

        try
        {
            var response = await SendWithTimeoutAsync<ReceiveMessageRequest, ReceiveMessageResponse>(
                request,
                "AmazonSQS.ReceiveMessage",
                GetReceiveTimeout(request),
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled("ReceiveMessage");
        }
        catch (Exception ex)
        {
            Logger.ReceiveMessageFailed(ex, request.QueueUrl);
            return Error.Failure("SQS.ReceiveMessage.Failed", $"Failed to receive messages from SQS queue {request.QueueUrl}");
        }
    }

    public async Task<ErrorOr<DeleteMessageResponse>> DeleteMessageAsync(DeleteMessageRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.QueueUrl))
            return Error.Validation("DeleteMessageRequest.QueueUrl", "Queue URL is required.");

        if (string.IsNullOrWhiteSpace(request.ReceiptHandle))
            return Error.Validation("DeleteMessageRequest.ReceiptHandle", "Receipt handle is required.");

        try
        {
            var response = await SendWithTimeoutAsync<DeleteMessageRequest, DeleteMessageResponse>(
                request,
                "AmazonSQS.DeleteMessage",
                Configuration.HttpTimeout,
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled("DeleteMessage");
        }
        catch (Exception ex)
        {
            Logger.DeleteMessageFailed(ex, request.QueueUrl);
            return Error.Failure("SQS.DeleteMessage.Failed", $"Failed to delete message from SQS queue {request.QueueUrl}");
        }
    }

    public async Task<ErrorOr<ChangeMessageVisibilityResponse>> ChangeMessageVisibilityAsync(ChangeMessageVisibilityRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.QueueUrl))
            return Error.Validation("ChangeMessageVisibilityRequest.QueueUrl", "Queue URL is required.");

        if (string.IsNullOrWhiteSpace(request.ReceiptHandle))
            return Error.Validation("ChangeMessageVisibilityRequest.ReceiptHandle", "Receipt handle is required.");

        if (request.VisibilityTimeout is < 0 or > MaxVisibilityTimeoutSeconds)
            return Error.Validation("ChangeMessageVisibilityRequest.VisibilityTimeout", "VisibilityTimeout must be between 0 and 43200 seconds.");

        try
        {
            var response = await SendWithTimeoutAsync<ChangeMessageVisibilityRequest, ChangeMessageVisibilityResponse>(
                request,
                "AmazonSQS.ChangeMessageVisibility",
                Configuration.HttpTimeout,
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled("ChangeMessageVisibility");
        }
        catch (Exception ex)
        {
            Logger.ChangeMessageVisibilityFailed(ex, request.QueueUrl);
            return Error.Failure("SQS.ChangeMessageVisibility.Failed", $"Failed to change message visibility in SQS queue {request.QueueUrl}");
        }
    }

    public async Task<ErrorOr<ChangeMessageVisibilityBatchResponse>> ChangeMessageVisibilityBatchAsync(ChangeMessageVisibilityBatchRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.QueueUrl))
            return Error.Validation("ChangeMessageVisibilityBatchRequest.QueueUrl", "Queue URL is required.");

        if (request.Entries is null || request.Entries.Count == 0)
            return Error.Validation("ChangeMessageVisibilityBatchRequest.Entries", "At least one entry is required.");

        if (request.Entries.Count > 10)
            return Error.Validation("ChangeMessageVisibilityBatchRequest.Entries", "Maximum 10 entries allowed per batch.");

        var ids = new HashSet<string>();
        foreach (var entry in request.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Id))
                return Error.Validation("ChangeMessageVisibilityBatchRequestEntry.Id", "Entry ID is required for all entries.");
            if (!ids.Add(entry.Id))
                return Error.Validation("ChangeMessageVisibilityBatchRequestEntry.Id", $"Duplicate entry ID: {entry.Id}");
            if (string.IsNullOrWhiteSpace(entry.ReceiptHandle))
                return Error.Validation("ChangeMessageVisibilityBatchRequestEntry.ReceiptHandle", "Receipt handle is required for all entries.");
            if (entry.VisibilityTimeout is < 0 or > MaxVisibilityTimeoutSeconds)
                return Error.Validation("ChangeMessageVisibilityBatchRequestEntry.VisibilityTimeout", "VisibilityTimeout must be between 0 and 43200 seconds.");
        }

        try
        {
            var response = await SendWithTimeoutAsync<ChangeMessageVisibilityBatchRequest, ChangeMessageVisibilityBatchResponse>(
                request,
                "AmazonSQS.ChangeMessageVisibilityBatch",
                Configuration.HttpTimeout,
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Cancelled("ChangeMessageVisibilityBatch");
        }
        catch (Exception ex)
        {
            Logger.ChangeMessageVisibilityBatchFailed(ex, request.QueueUrl);
            return Error.Failure("SQS.ChangeMessageVisibilityBatch.Failed", $"Failed to change message visibility batch in SQS queue {request.QueueUrl}");
        }
    }

    /// <summary>
    /// Sends a request with a per-request timeout. The named HttpClient has no timeout of its own so that
    /// long-polling receives can wait longer than other operations.
    /// </summary>
    private async Task<ApiResponse<TResponse>> SendWithTimeoutAsync<TRequest, TResponse>(TRequest request, string target, TimeSpan timeout, CancellationToken cancellationToken)
        where TResponse : class
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            return await SendAsync<TRequest, TResponse>(HttpMethod.Post, "/", request, target, timeoutCts.Token);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException($"The {target} request did not complete within {timeout.TotalSeconds:0.###} seconds.", ex);
        }
    }

    /// <summary>
    /// A receive may legitimately wait up to <c>WaitTimeSeconds</c> for messages, so it gets that long on top of
    /// <see cref="Core.Configuration.AwsServiceConfiguration.HttpTimeout"/>.
    /// </summary>
    internal TimeSpan GetReceiveTimeout(ReceiveMessageRequest request)
    {
        if (Configuration.HttpTimeout == Timeout.InfiniteTimeSpan)
            return Timeout.InfiniteTimeSpan;

        return Configuration.HttpTimeout + TimeSpan.FromSeconds(Math.Clamp(request.WaitTimeSeconds ?? 0, 0, MaxWaitTimeSeconds));
    }

    private static Error Cancelled(string operation) =>
        Error.Failure($"SQS.{operation}.Cancelled", $"The {operation} request was cancelled.");

    private static ErrorOr<T> ConvertApiResponse<T>(ApiResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return response.Value!;
        }

        var error = response.Error!;
        var sqsError = Error.Failure(
            code: $"Goa.SQS.{error.Type ?? error.Code ?? "Unknown"}",
            description: error.Message ?? "An error occurred while processing the request.");

        return sqsError;
    }
}
