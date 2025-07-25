using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Dynamo.Errors;
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
public class DynamoServiceClient : AwsServiceClient<DynamoServiceClientConfiguration>, IDynamoClient
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

    /// <summary>
    /// Gets an item from a DynamoDB table.
    /// </summary>
    /// <param name="request">The get item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The get item response, or an error if the operation failed.</returns>
    public async Task<ErrorOr<GetItemResponse>> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default)
    {
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.GetItemRequest,
            DynamoJsonContext.Default.GetItemResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.PutItemRequest,
            DynamoJsonContext.Default.PutItemResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.UpdateItemRequest,
            DynamoJsonContext.Default.UpdateItemResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.DeleteItemRequest,
            DynamoJsonContext.Default.DeleteItemResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.QueryRequest,
            DynamoJsonContext.Default.QueryResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.ScanRequest,
            DynamoJsonContext.Default.ScanResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.BatchGetItemRequest,
            DynamoJsonContext.Default.BatchGetItemResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.BatchWriteItemRequest,
            DynamoJsonContext.Default.BatchWriteItemResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.TransactWriteRequest,
            DynamoJsonContext.Default.TransactWriteItemResponse,
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
        var response = await SendAsync(
            HttpMethod.Post,
            "/",
            request,
            DynamoJsonContext.Default.TransactGetRequest,
            DynamoJsonContext.Default.TransactGetItemResponse,
            "DynamoDB_20120810.TransactGetItems",
            cancellationToken);

        return ConvertApiResponse(response);
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
