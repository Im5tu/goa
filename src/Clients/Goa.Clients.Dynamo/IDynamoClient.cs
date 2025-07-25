using ErrorOr;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;
using Goa.Clients.Dynamo.Operations.UpdateItem;

namespace Goa.Clients.Dynamo;

/// <summary>
/// High-performance DynamoDB client interface optimized for AWS Lambda usage.
/// All operations use strongly-typed request objects and return ErrorOr results.
/// </summary>
public interface IDynamoClient
{
    /// <summary>
    /// Gets an item from a DynamoDB table.
    /// </summary>
    /// <param name="request">The get item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The get item response, or an error if the operation failed.</returns>
    Task<ErrorOr<GetItemResponse>> GetItemAsync(GetItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Puts an item into a DynamoDB table.
    /// </summary>
    /// <param name="request">The put item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The put item response, or an error if the operation failed.</returns>
    Task<ErrorOr<PutItemResponse>> PutItemAsync(PutItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an item in a DynamoDB table.
    /// </summary>
    /// <param name="request">The update item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The update item response, or an error if the operation failed.</returns>
    Task<ErrorOr<UpdateItemResponse>> UpdateItemAsync(UpdateItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an item from a DynamoDB table.
    /// </summary>
    /// <param name="request">The delete item request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The delete item response, or an error if the operation failed.</returns>
    Task<ErrorOr<DeleteItemResponse>> DeleteItemAsync(DeleteItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries items from a DynamoDB table.
    /// </summary>
    /// <param name="request">The query request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The query response containing items and pagination information, or an error if the operation failed.</returns>
    Task<ErrorOr<QueryResponse>> QueryAsync(QueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans items from a DynamoDB table.
    /// </summary>
    /// <param name="request">The scan request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The scan response containing items and pagination information, or an error if the operation failed.</returns>
    Task<ErrorOr<ScanResponse>> ScanAsync(ScanRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets multiple items from DynamoDB tables in a single batch operation.
    /// </summary>
    /// <param name="request">The batch get request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The batch get response containing items and unprocessed keys, or an error if the operation failed.</returns>
    Task<ErrorOr<BatchGetItemResponse>> BatchGetItemAsync(BatchGetItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes multiple items to DynamoDB tables in a single batch operation.
    /// </summary>
    /// <param name="request">The batch write request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The batch write response indicating success and any unprocessed items, or an error if the operation failed.</returns>
    Task<ErrorOr<BatchWriteItemResponse>> BatchWriteItemAsync(BatchWriteItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a transactional write operation with multiple operations that either all succeed or all fail.
    /// </summary>
    /// <param name="request">The transact write request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The transact write response, or an error if the transaction failed.</returns>
    Task<ErrorOr<TransactWriteItemResponse>> TransactWriteItemsAsync(TransactWriteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a transactional get operation to retrieve multiple items atomically.
    /// </summary>
    /// <param name="request">The transact get request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The transact get response with items in the same order as the requests, or an error if the operation failed.</returns>
    Task<ErrorOr<TransactGetItemResponse>> TransactGetItemsAsync(TransactGetRequest request, CancellationToken cancellationToken = default);
}
