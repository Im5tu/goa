using System.Text.Json;
using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Dynamo.Errors;
using Goa.Clients.Dynamo.Internal;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;
using Goa.Clients.Dynamo.Operations.UpdateItem;
using Goa.Clients.Dynamo.Serialization;
using Microsoft.Extensions.Logging;

namespace Goa.Clients.Dynamo;

/// <summary>
/// High-performance DynamoDB service client that implements IDynamoClient using AWS service client infrastructure.
/// Provides strongly-typed DynamoDB operations with built-in error handling, logging, and AWS authentication.
/// </summary>
public class DynamoServiceClient : JsonAwsServiceClient<DynamoServiceClientConfiguration>, IDynamoClient
{
    /// <summary>
    /// Initializes a new instance of the DynamoServiceClient class.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="logger">Logger instance for logging operations.</param>
    /// <param name="configuration">Configuration for the DynamoDB service.</param>
    public DynamoServiceClient(IHttpClientFactory httpClientFactory, ILogger<DynamoServiceClient> logger, DynamoServiceClientConfiguration configuration)
        : base(httpClientFactory, logger, configuration)
    {
    }

    /// <inheritdoc />
    protected override System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue> ResolveJsonTypeInfo<TValue>()
    {
        return DynamoJsonContext.Default.GetTypeInfo(typeof(TValue))
            as System.Text.Json.Serialization.Metadata.JsonTypeInfo<TValue>
            ?? throw new InvalidOperationException($"Cannot find type {typeof(TValue).Name} in serialization context");
    }

    /// <summary>
    /// Gets an item from a DynamoDB table.
    /// </summary>
    /// <param name="request">The get item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The get item response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<GetItemResponse>> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<GetItemRequest, GetItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.GetItem",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Puts an item into a DynamoDB table.
    /// </summary>
    /// <param name="request">The put item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The put item response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<PutItemResponse>> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<PutItemRequest, PutItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.PutItem",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Updates an item in a DynamoDB table.
    /// </summary>
    /// <param name="request">The update item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The update item response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<UpdateItemResponse>> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<UpdateItemRequest, UpdateItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.UpdateItem",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Deletes an item from a DynamoDB table.
    /// </summary>
    /// <param name="request">The delete item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The delete item response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<DeleteItemResponse>> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<DeleteItemRequest, DeleteItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.DeleteItem",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Queries items from a DynamoDB table.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The query response containing items and pagination information, or an error if the operation failed.</returns>
    public async Task<ErrorOr<QueryResponse>> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<QueryRequest, QueryResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.Query",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Scans items from a DynamoDB table.
    /// </summary>
    /// <param name="request">The scan request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The scan response containing items and pagination information, or an error if the operation failed.</returns>
    public async Task<ErrorOr<ScanResponse>> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<ScanRequest, ScanResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.Scan",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Gets multiple items from DynamoDB tables in a single batch operation.
    /// </summary>
    /// <param name="request">The batch get request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The batch get response containing items and unprocessed keys, or an error if the operation failed.</returns>
    public async Task<ErrorOr<BatchGetItemResponse>> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<BatchGetItemRequest, BatchGetItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.BatchGetItem",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Writes multiple items to DynamoDB tables in a single batch operation.
    /// </summary>
    /// <param name="request">The batch write request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The batch write response indicating success and any unprocessed items, or an error if the operation failed.</returns>
    public async Task<ErrorOr<BatchWriteItemResponse>> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<BatchWriteItemRequest, BatchWriteItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.BatchWriteItem",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Executes a transactional write operation with multiple operations that either all succeed or all fail.
    /// </summary>
    /// <param name="request">The transact write request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The transact write response, or an error if the transaction failed.</returns>
    public async Task<ErrorOr<TransactWriteItemResponse>> TransactWriteItemsAsync(TransactWriteRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<TransactWriteRequest, TransactWriteItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.TransactWriteItems",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <summary>
    /// Executes a transactional get operation to retrieve multiple items atomically.
    /// </summary>
    /// <param name="request">The transact get request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The transact get response with items in the same order as the requests, or an error if the operation failed.</returns>
    public async Task<ErrorOr<TransactGetItemResponse>> TransactGetItemsAsync(TransactGetRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync<TransactGetRequest, TransactGetItemResponse>(
            HttpMethod.Post,
            "/",
            request,
            "DynamoDB_20120810.TransactGetItems",
            cancellationToken);

        return ConvertApiResponse(response);
    }

    /// <inheritdoc/>
    public async Task<ErrorOr<QueryResult<T>>> QueryAsync<T>(QueryRequest request, DynamoItemReader<T> itemReader, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(request, ResolveJsonTypeInfo<QueryRequest>());
        using var requestMessage = CreateRequestMessage(
            HttpMethod.Post, "/", content,
            JsonContentType);
        requestMessage.Headers.Add("X-Amz-Target", "DynamoDB_20120810.Query");

        using var response = await SendAsync(requestMessage, "DynamoDB_20120810.Query", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return await HandleTypedErrorAsync(response, cancellationToken);

        using var buffer = await ReadResponseBytesAsync(response, cancellationToken);
        if (buffer.Length == 0)
            return new QueryResult<T>();

        return DynamoResponseReader.ReadQueryResponse(buffer.Span, itemReader);
    }

    /// <inheritdoc/>
    public async Task<ErrorOr<ScanResult<T>>> ScanAsync<T>(ScanRequest request, DynamoItemReader<T> itemReader, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(request, ResolveJsonTypeInfo<ScanRequest>());
        using var requestMessage = CreateRequestMessage(
            HttpMethod.Post, "/", content,
            JsonContentType);
        requestMessage.Headers.Add("X-Amz-Target", "DynamoDB_20120810.Scan");

        using var response = await SendAsync(requestMessage, "DynamoDB_20120810.Scan", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return await HandleTypedErrorAsync(response, cancellationToken);

        using var buffer = await ReadResponseBytesAsync(response, cancellationToken);
        if (buffer.Length == 0)
            return new ScanResult<T>();

        return DynamoResponseReader.ReadScanResponse(buffer.Span, itemReader);
    }

    /// <inheritdoc/>
    public async Task<ErrorOr<T?>> GetItemAsync<T>(GetItemRequest request, DynamoItemReader<T> itemReader, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(request, ResolveJsonTypeInfo<GetItemRequest>());
        using var requestMessage = CreateRequestMessage(
            HttpMethod.Post, "/", content,
            JsonContentType);
        requestMessage.Headers.Add("X-Amz-Target", "DynamoDB_20120810.GetItem");

        using var response = await SendAsync(requestMessage, "DynamoDB_20120810.GetItem", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return await HandleTypedErrorAsync(response, cancellationToken);

        using var buffer = await ReadResponseBytesAsync(response, cancellationToken);
        if (buffer.Length == 0)
            return default(T);

        return DynamoResponseReader.ReadGetItemResponse(buffer.Span, itemReader);
    }

    /// <inheritdoc/>
    public async Task<ErrorOr<PutItemResponse>> PutItemAsync<T>(string tableName, T item, DynamoItemWriter<T> itemWriter, CancellationToken cancellationToken = default)
    {
        var bufferWriter = new System.Buffers.ArrayBufferWriter<byte>(256);
        using var writer = new Utf8JsonWriter(bufferWriter);
        writer.WriteStartObject();
        writer.WriteString("TableName", tableName);
        writer.WritePropertyName("Item");
        itemWriter(writer, item);
        writer.WriteEndObject();
        writer.Flush();

        var content = bufferWriter.WrittenSpan.ToArray();
        using var requestMessage = CreateRequestMessage(
            HttpMethod.Post, "/", content,
            JsonContentType);
        requestMessage.Headers.Add("X-Amz-Target", "DynamoDB_20120810.PutItem");

        using var response = await SendAsync(requestMessage, "DynamoDB_20120810.PutItem", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return await HandleTypedErrorAsync(response, cancellationToken);

        using var responseBuffer = await ReadResponseBytesAsync(response, cancellationToken);
        if (responseBuffer.Length == 0)
            return new PutItemResponse();

        var jsonReader = new Utf8JsonReader(responseBuffer.Span);
        return JsonSerializer.Deserialize(ref jsonReader, ResolveJsonTypeInfo<PutItemResponse>())!;
    }

    /// <inheritdoc/>
    public async Task<ErrorOr<BatchGetResult<T>>> BatchGetItemAsync<T>(BatchGetItemRequest request, DynamoItemReader<T> itemReader, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(request, ResolveJsonTypeInfo<BatchGetItemRequest>());
        using var requestMessage = CreateRequestMessage(
            HttpMethod.Post, "/", content,
            JsonContentType);
        requestMessage.Headers.Add("X-Amz-Target", "DynamoDB_20120810.BatchGetItem");

        using var response = await SendAsync(requestMessage, "DynamoDB_20120810.BatchGetItem", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return await HandleTypedErrorAsync(response, cancellationToken);

        using var buffer = await ReadResponseBytesAsync(response, cancellationToken);
        if (buffer.Length == 0)
            return new BatchGetResult<T>();

        return DynamoResponseReader.ReadBatchGetItemResponse(buffer.Span, itemReader);
    }

    /// <inheritdoc/>
    public async Task<ErrorOr<TransactGetResult<T>>> TransactGetItemsAsync<T>(TransactGetRequest request, DynamoItemReader<T> itemReader, CancellationToken cancellationToken = default)
    {
        var content = JsonSerializer.SerializeToUtf8Bytes(request, ResolveJsonTypeInfo<TransactGetRequest>());
        using var requestMessage = CreateRequestMessage(
            HttpMethod.Post, "/", content,
            JsonContentType);
        requestMessage.Headers.Add("X-Amz-Target", "DynamoDB_20120810.TransactGetItems");

        using var response = await SendAsync(requestMessage, "DynamoDB_20120810.TransactGetItems", cancellationToken);

        if (!response.IsSuccessStatusCode)
            return await HandleTypedErrorAsync(response, cancellationToken);

        using var buffer = await ReadResponseBytesAsync(response, cancellationToken);
        if (buffer.Length == 0)
            return new TransactGetResult<T>();

        return DynamoResponseReader.ReadTransactGetItemResponse(buffer.Span, itemReader);
    }

    private async Task<Error> HandleTypedErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(errorPayload))
            return Error.Failure("Goa.DynamoDb.Unknown", "Request not successful.");

        var error = DeserializeJsonError(errorPayload);
        if (error is not null)
        {
            error = error with { Payload = errorPayload, StatusCode = response.StatusCode };
            error = ProcessAwsErrorHeaders(response, error);
        }

        var errorType = error?.Type ?? error?.Code ?? "Unknown";
        return Error.Failure(
            code: MapErrorCodeToDynamo(errorType),
            description: error?.Message ?? "An error occurred while processing the request.");
    }

    /// <summary>
    /// Converts an ApiResponse to an ErrorOr result, mapping AWS-specific error codes to DynamoDB error codes.
    /// </summary>
    /// <typeparam name="T">The response type.</typeparam>
    /// <param name="response">The API response to convert.</param>
    /// <returns>An ErrorOr containing either the response data or mapped errors.</returns>
    private static ErrorOr<T> ConvertApiResponse<T>(ApiResponse<T> response)
    {
        if (response.IsSuccess)
        {
            return response.Value!;
        }

        // Map AWS error codes to DynamoDB error codes with Goa.DynamoDb prefix
        var error = response.Error!;
        var dynamoError = Error.Failure(
            code: MapErrorCodeToDynamo(error.Type ?? error.Code ?? "Unknown"),
            description: error.Message ?? "An error occurred while processing the request.");

        return dynamoError;
    }

    /// <summary>
    /// Maps AWS DynamoDB error types to Goa.DynamoDb prefixed error codes.
    /// </summary>
    /// <param name="awsErrorType">The AWS error type or code.</param>
    /// <returns>A Goa.DynamoDb prefixed error code.</returns>
    private static string MapErrorCodeToDynamo(string awsErrorType)
    {
        return awsErrorType switch
        {
            "ConditionalCheckFailedException" => DynamoErrorCodes.ConditionalCheckFailedException,
            "ProvisionedThroughputExceededException" => DynamoErrorCodes.ProvisionedThroughputExceededException,
            "ResourceNotFoundException" => DynamoErrorCodes.ResourceNotFoundException,
            "ItemNotFoundException" => DynamoErrorCodes.ItemNotFound,
            "RequestLimitExceededException" => DynamoErrorCodes.RequestLimitExceeded,
            "ThrottlingException" => DynamoErrorCodes.ThrottlingException,
            "TooManyRequestsException" => DynamoErrorCodes.TooManyRequestsException,
            "TransactionConflictException" => DynamoErrorCodes.TransactionConflictException,
            "TransactionCanceledException" => DynamoErrorCodes.TransactionCanceledException,
            "TransactionInProgressException" => DynamoErrorCodes.TransactionInProgressException,
            "ReplicatedWriteConflictException" => DynamoErrorCodes.ReplicatedWriteConflictException,
            "ValidationException" => DynamoErrorCodes.ValidationException,
            "InvalidParameterValueException" => DynamoErrorCodes.InvalidParameterValueException,
            "MissingParameterException" => DynamoErrorCodes.MissingParameterException,
            "UnauthorizedException" => DynamoErrorCodes.UnauthorizedException,
            "AccessDeniedException" => DynamoErrorCodes.AccessDeniedException,
            "NotAuthorizedException" => DynamoErrorCodes.NotAuthorizedException,
            "InvalidUserPoolConfigurationException" => DynamoErrorCodes.InvalidUserPoolConfigurationException,
            "InternalServerError" => DynamoErrorCodes.InternalServerError,
            "ServiceUnavailable" => DynamoErrorCodes.ServiceUnavailable,
            "RequestTimeoutException" => DynamoErrorCodes.RequestTimeoutException,
            "ResourceInUseException" => DynamoErrorCodes.ResourceInUseException,
            "TableNotFoundException" => DynamoErrorCodes.TableNotFoundException,
            "IndexNotFoundException" => DynamoErrorCodes.IndexNotFoundException,
            "ItemCollectionSizeLimitExceededException" => DynamoErrorCodes.ItemCollectionSizeLimitExceededException,
            _ => $"Goa.DynamoDb.{awsErrorType}" // Fallback: prefix unknown error types
        };
    }
}
