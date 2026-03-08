using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Order;
using EfficientDynamoDb.Operations.Shared;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using EfficientAttributeValue = EfficientDynamoDb.DocumentModel.AttributeValue;
using EfficientUpdateItemRequest = EfficientDynamoDb.Operations.UpdateItem.UpdateItemRequest;
using EfficientUpdateItemResponse = EfficientDynamoDb.Operations.UpdateItem.UpdateItemResponse;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class UpdateItemAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();

        // Seed items for update benchmarks
        _fixture.SeedItemsAsync("update-bench", 10).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Update Single Attribute")]
    public async Task<UpdateItemResponse> AwsSdk_UpdateItem_SingleAttribute()
    {
        return await _fixture.AwsSdkClient.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue("update-bench"),
                ["sk"] = new AttributeValue("item-0000")
            },
            UpdateExpression = "SET #d = :val",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "data"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":val"] = new AttributeValue("updated-value")
            }
        });
    }

    [Benchmark, BenchmarkCategory("Update Single Attribute")]
    public async Task<bool> Goa_UpdateItem_SingleAttribute()
    {
        var response = await _fixture.GoaClient.UpdateItemAsync(new Goa.Clients.Dynamo.Operations.UpdateItem.UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("update-bench"),
                ["sk"] = GoaModels.AttributeValue.String("item-0000")
            },
            UpdateExpression = "SET #d = :val",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "data"
            },
            ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
            {
                [":val"] = GoaModels.AttributeValue.String("updated-value")
            }
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Update Single Attribute")]
    public async Task<EfficientUpdateItemResponse> Efficient_UpdateItem_SingleAttribute()
    {
        return await _fixture.EfficientClient.UpdateItemAsync(new EfficientUpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", "update-bench", "sk", "item-0000"),
            UpdateExpression = "SET #d = :val",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "data"
            },
            ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
            {
                [":val"] = "updated-value"
            }
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Update Multiple Attributes")]
    public async Task<UpdateItemResponse> AwsSdk_UpdateItem_MultipleAttributes()
    {
        return await _fixture.AwsSdkClient.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue("update-bench"),
                ["sk"] = new AttributeValue("item-0001")
            },
            UpdateExpression = "SET #d = :val, #s = :status, #n = #n + :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "data",
                ["#s"] = "status",
                ["#n"] = "number"
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":val"] = new AttributeValue("multi-updated"),
                [":status"] = new AttributeValue("modified"),
                [":inc"] = new AttributeValue { N = "1" }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Update Multiple Attributes")]
    public async Task<bool> Goa_UpdateItem_MultipleAttributes()
    {
        var response = await _fixture.GoaClient.UpdateItemAsync(new Goa.Clients.Dynamo.Operations.UpdateItem.UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("update-bench"),
                ["sk"] = GoaModels.AttributeValue.String("item-0001")
            },
            UpdateExpression = "SET #d = :val, #s = :status, #n = #n + :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "data",
                ["#s"] = "status",
                ["#n"] = "number"
            },
            ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
            {
                [":val"] = GoaModels.AttributeValue.String("multi-updated"),
                [":status"] = GoaModels.AttributeValue.String("modified"),
                [":inc"] = GoaModels.AttributeValue.Number("1")
            }
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Update Multiple Attributes")]
    public async Task<EfficientUpdateItemResponse> Efficient_UpdateItem_MultipleAttributes()
    {
        return await _fixture.EfficientClient.UpdateItemAsync(new EfficientUpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", "update-bench", "sk", "item-0001"),
            UpdateExpression = "SET #d = :val, #s = :status, #n = #n + :inc",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#d"] = "data",
                ["#s"] = "status",
                ["#n"] = "number"
            },
            ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
            {
                [":val"] = "multi-updated",
                [":status"] = "modified",
                [":inc"] = new EfficientDynamoDb.DocumentModel.NumberAttributeValue("1")
            }
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Update With Return Values")]
    public async Task<UpdateItemResponse> AwsSdk_UpdateItem_WithReturnValues()
    {
        return await _fixture.AwsSdkClient.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["pk"] = new AttributeValue("update-bench"),
                ["sk"] = new AttributeValue("item-0002")
            },
            UpdateExpression = "SET #d = :val",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":val"] = new AttributeValue("return-val")
            },
            ReturnValues = ReturnValue.ALL_NEW
        });
    }

    [Benchmark, BenchmarkCategory("Update With Return Values")]
    public async Task<GoaModels.DynamoRecord?> Goa_UpdateItem_WithReturnValues()
    {
        var response = await _fixture.GoaClient.UpdateItemAsync(new Goa.Clients.Dynamo.Operations.UpdateItem.UpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String("update-bench"),
                ["sk"] = GoaModels.AttributeValue.String("item-0002")
            },
            UpdateExpression = "SET #d = :val",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
            ExpressionAttributeValues = new Dictionary<string, GoaModels.AttributeValue>
            {
                [":val"] = GoaModels.AttributeValue.String("return-val")
            },
            ReturnValues = Goa.Clients.Dynamo.Enums.ReturnValues.ALL_NEW
        });
        return response.Value.Attributes;
    }

    [Benchmark, BenchmarkCategory("Update With Return Values")]
    public async Task<EfficientUpdateItemResponse> Efficient_UpdateItem_WithReturnValues()
    {
        return await _fixture.EfficientClient.UpdateItemAsync(new EfficientUpdateItemRequest
        {
            TableName = _fixture.TableName,
            Key = new PrimaryKey("pk", "update-bench", "sk", "item-0002"),
            UpdateExpression = "SET #d = :val",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#d"] = "data" },
            ExpressionAttributeValues = new Dictionary<string, EfficientAttributeValue>
            {
                [":val"] = "return-val"
            },
            ReturnValues = EfficientDynamoDb.Operations.Shared.ReturnValues.AllNew
        });
    }
}
