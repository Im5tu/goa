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
            var response = await SendAsync<SendMessageRequest, SendMessageResponse>(
                HttpMethod.Post,
                "/",
                request,
                "AmazonSQS.SendMessage",
                cancellationToken);

            return ConvertApiResponse(response);
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
            var response = await SendAsync<SendMessageBatchRequest, SendMessageBatchResponse>(
                HttpMethod.Post,
                "/",
                request,
                "AmazonSQS.SendMessageBatch",
                cancellationToken);

            return ConvertApiResponse(response);
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
            var response = await SendAsync<ReceiveMessageRequest, ReceiveMessageResponse>(
                HttpMethod.Post,
                "/",
                request,
                "AmazonSQS.ReceiveMessage",
                cancellationToken);

            return ConvertApiResponse(response);
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
            var response = await SendAsync<DeleteMessageRequest, DeleteMessageResponse>(
                HttpMethod.Post,
                "/",
                request,
                "AmazonSQS.DeleteMessage",
                cancellationToken);

            return ConvertApiResponse(response);
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
            var response = await SendAsync<ChangeMessageVisibilityRequest, ChangeMessageVisibilityResponse>(
                HttpMethod.Post,
                "/",
                request,
                "AmazonSQS.ChangeMessageVisibility",
                cancellationToken);

            return ConvertApiResponse(response);
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
            var response = await SendAsync<ChangeMessageVisibilityBatchRequest, ChangeMessageVisibilityBatchResponse>(
                HttpMethod.Post,
                "/",
                request,
                "AmazonSQS.ChangeMessageVisibilityBatch",
                cancellationToken);

            return ConvertApiResponse(response);
        }
        catch (Exception ex)
        {
            Logger.ChangeMessageVisibilityBatchFailed(ex, request.QueueUrl);
            return Error.Failure("SQS.ChangeMessageVisibilityBatch.Failed", $"Failed to change message visibility batch in SQS queue {request.QueueUrl}");
        }
    }

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
