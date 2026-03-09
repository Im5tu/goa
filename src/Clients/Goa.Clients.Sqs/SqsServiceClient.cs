using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Sqs.Operations.DeleteMessage;
using Goa.Clients.Sqs.Operations.ReceiveMessage;
using Goa.Clients.Sqs.Operations.SendMessage;
using Goa.Clients.Sqs.Operations.SendMessageBatch;
using Goa.Clients.Sqs.Serialization;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Sqs;

internal sealed class SqsServiceClient : JsonAwsServiceClient<SqsServiceClientConfiguration>, ISqsClient
{
    public SqsServiceClient(
        IHttpClientFactory httpClientFactory,
        SqsServiceClientConfiguration configuration,
        ILogger<SqsServiceClient> logger)
        : base(httpClientFactory, logger, configuration)
    {
    }

    protected override void WriteRequestBody<TRequest>(System.Buffers.IBufferWriter<byte> buffer, TRequest request)
    {
        var typeInfo = SqsJsonContext.Default.GetTypeInfo(typeof(TRequest))
            as System.Text.Json.Serialization.Metadata.JsonTypeInfo<TRequest>
            ?? throw new InvalidOperationException($"Cannot find type {typeof(TRequest).Name} in serialization context");
        using var writer = new System.Text.Json.Utf8JsonWriter(buffer);
        System.Text.Json.JsonSerializer.Serialize(writer, request, typeInfo);
    }

    protected override TResponse? ReadJsonResponse<TResponse>(ref System.Text.Json.Utf8JsonReader reader) where TResponse : class
    {
        var typeInfo = SqsJsonContext.Default.GetTypeInfo(typeof(TResponse))
            as System.Text.Json.Serialization.Metadata.JsonTypeInfo<TResponse>
            ?? throw new InvalidOperationException($"Cannot find type {typeof(TResponse).Name} in serialization context");
        return System.Text.Json.JsonSerializer.Deserialize(ref reader, typeInfo);
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
            Logger.LogError(ex, "Failed to send message to SQS queue {QueueUrl}", request.QueueUrl);
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
            Logger.LogError(ex, "Failed to send message batch to SQS queue {QueueUrl}", request.QueueUrl);
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
            Logger.LogError(ex, "Failed to receive messages from SQS queue {QueueUrl}", request.QueueUrl);
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
            Logger.LogError(ex, "Failed to delete message from SQS queue {QueueUrl}", request.QueueUrl);
            return Error.Failure("SQS.DeleteMessage.Failed", $"Failed to delete message from SQS queue {request.QueueUrl}");
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
