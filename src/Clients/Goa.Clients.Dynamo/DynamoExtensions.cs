using ErrorOr;
using Goa.Clients.Dynamo.Models;
using Goa.Clients.Dynamo.Operations.Batch;
using Goa.Clients.Dynamo.Operations.DeleteItem;
using Goa.Clients.Dynamo.Operations.GetItem;
using Goa.Clients.Dynamo.Operations.PutItem;
using Goa.Clients.Dynamo.Operations.Query;
using Goa.Clients.Dynamo.Operations.Scan;
using Goa.Clients.Dynamo.Operations.Transactions;
using Goa.Clients.Dynamo.Operations.UpdateItem;
using System.Runtime.CompilerServices;

namespace Goa.Clients.Dynamo;

/// <summary>
/// Extension methods for IDynamoClient to provide fluent query building capabilities.
/// </summary>
public static class DynamoExtensions
{
    /// <summary>
    /// Executes a DynamoDB GetItem operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to get the item from.</param>
    /// <param name="builder">Action to configure the get operation using GetItemBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous get operation.</returns>
    public static Task<ErrorOr<GetItemResponse>> GetItemAsync(this IDynamoClient client, string tableName, Action<GetItemBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new GetItemBuilder(tableName);
        builder(_builder);

        return client.GetItemAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB Query operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to query.</param>
    /// <param name="builder">Action to configure the query using QueryBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous query operation.</returns>
    public static Task<ErrorOr<QueryResponse>> QueryAsync(this IDynamoClient client, string tableName, Action<QueryBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new QueryBuilder(tableName);
        builder(_builder);

        return client.QueryAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB Scan operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to scan.</param>
    /// <param name="builder">Action to configure the scan using ScanBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous scan operation.</returns>
    public static Task<ErrorOr<ScanResponse>> ScanAsync(this IDynamoClient client, string tableName, Action<ScanBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new ScanBuilder(tableName);
        builder(_builder);

        return client.ScanAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB PutItem operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to put the item into.</param>
    /// <param name="builder">Action to configure the put operation using PutItemBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous put operation.</returns>
    public static Task<ErrorOr<PutItemResponse>> PutItemAsync(this IDynamoClient client, string tableName, Action<PutItemBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new PutItemBuilder(tableName);
        builder(_builder);

        return client.PutItemAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB UpdateItem operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to update the item in.</param>
    /// <param name="builder">Action to configure the update operation using UpdateItemBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous update operation.</returns>
    public static Task<ErrorOr<UpdateItemResponse>> UpdateItemAsync(this IDynamoClient client, string tableName, Action<UpdateItemBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new UpdateItemBuilder(tableName);
        builder(_builder);

        return client.UpdateItemAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB DeleteItem operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to delete the item from.</param>
    /// <param name="builder">Action to configure the delete operation using DeleteItemBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public static Task<ErrorOr<DeleteItemResponse>> DeleteItemAsync(this IDynamoClient client, string tableName, Action<DeleteItemBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new DeleteItemBuilder(tableName);
        builder(_builder);

        return client.DeleteItemAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB BatchGetItem operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="builder">Action to configure the batch get operation using BatchGetItemBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous batch get operation.</returns>
    public static Task<ErrorOr<BatchGetItemResponse>> BatchGetItemAsync(this IDynamoClient client, Action<BatchGetItemBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new BatchGetItemBuilder();
        builder(_builder);

        return client.BatchGetItemAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB BatchWriteItem operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="builder">Action to configure the batch write operation using BatchWriteItemBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous batch write operation.</returns>
    public static Task<ErrorOr<BatchWriteItemResponse>> BatchWriteItemAsync(this IDynamoClient client, Action<BatchWriteItemBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new BatchWriteItemBuilder();
        builder(_builder);

        return client.BatchWriteItemAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB Query operation with automatic pagination using an async iterator.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to query.</param>
    /// <param name="builder">Action to configure the query using QueryBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable that yields items from all pages of the query result.</returns>
    public static async IAsyncEnumerable<DynamoRecord> QueryAllAsync(this IDynamoClient client, string tableName, Action<QueryBuilder> builder, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var _builder = new QueryBuilder(tableName);
        builder(_builder);
        var request = _builder.Build();

        do
        {
            var result = await client.QueryAsync(request, cancellationToken);
            if (result.IsError)
            {
                yield break;
            }

            foreach (var item in result.Value.Items)
            {
                yield return item;
            }

            if (!result.Value.HasMoreResults)
            {
                break;
            }

            request.ExclusiveStartKey = result.Value.LastEvaluatedKey;
        }
        while (true);
    }

    /// <summary>
    /// Executes a DynamoDB Scan operation with automatic pagination using an async iterator.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="tableName">The name of the table to scan.</param>
    /// <param name="builder">Action to configure the scan using ScanBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable that yields items from all pages of the scan result.</returns>
    public static async IAsyncEnumerable<DynamoRecord> ScanAllAsync(this IDynamoClient client, string tableName, Action<ScanBuilder> builder, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var _builder = new ScanBuilder(tableName);
        builder(_builder);
        var request = _builder.Build();

        do
        {
            var result = await client.ScanAsync(request, cancellationToken);
            if (result.IsError)
            {
                yield break;
            }

            foreach (var item in result.Value.Items)
            {
                yield return item;
            }

            if (!result.Value.HasMoreResults)
            {
                break;
            }

            request.ExclusiveStartKey = result.Value.LastEvaluatedKey;
        }
        while (true);
    }

    /// <summary>
    /// Executes a DynamoDB BatchGetItem operation with automatic retry of unprocessed keys using an async iterator.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="builder">Action to configure the batch get operation using BatchGetItemBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable that yields items from all tables in the batch get result, retrying unprocessed keys.</returns>
    public static async IAsyncEnumerable<KeyValuePair<string, DynamoRecord>> BatchGetAllAsync(this IDynamoClient client, Action<BatchGetItemBuilder> builder, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var _builder = new BatchGetItemBuilder();
        builder(_builder);
        var request = _builder.Build();

        do
        {
            var result = await client.BatchGetItemAsync(request, cancellationToken);
            if (result.IsError)
            {
                yield break;
            }

            foreach (var tableResponse in result.Value.Responses)
            {
                foreach (var item in tableResponse.Value)
                {
                    yield return new KeyValuePair<string, DynamoRecord>(tableResponse.Key, item);
                }
            }

            if (!result.Value.HasUnprocessedKeys)
            {
                break;
            }

            // Retry with unprocessed keys
            request.RequestItems = result.Value.UnprocessedKeys!;
        }
        while (true);
    }

    /// <summary>
    /// Converts an async enumerable to a list asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of elements in the async enumerable.</typeparam>
    /// <param name="source">The async enumerable to convert.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of all elements.</returns>
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// Executes a DynamoDB TransactWrite operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="builder">Action to configure the transaction using TransactWriteBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous transaction operation.</returns>
    public static Task<ErrorOr<TransactWriteItemResponse>> TransactWriteItemsAsync(this IDynamoClient client, Action<TransactWriteBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new TransactWriteBuilder();
        builder(_builder);

        return client.TransactWriteItemsAsync(_builder.Build(), cancellationToken);
    }

    /// <summary>
    /// Executes a DynamoDB TransactGet operation using a fluent builder pattern.
    /// </summary>
    /// <param name="client">The DynamoDB client instance.</param>
    /// <param name="builder">Action to configure the transaction using TransactGetBuilder.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous transaction operation.</returns>
    public static Task<ErrorOr<TransactGetItemResponse>> TransactGetItemsAsync(this IDynamoClient client, Action<TransactGetBuilder> builder, CancellationToken cancellationToken = default)
    {
        var _builder = new TransactGetBuilder();
        builder(_builder);

        return client.TransactGetItemsAsync(_builder.Build(), cancellationToken);
    }
}
