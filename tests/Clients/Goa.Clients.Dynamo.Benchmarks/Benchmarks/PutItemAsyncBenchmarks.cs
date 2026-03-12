using BenchmarkDotNet.Order;
using EfficientDynamoDb.DocumentModel;
using Goa.Clients.Dynamo.Benchmarks.Infrastructure;
using GoaModels = Goa.Clients.Dynamo.Models;
using AwsAttributeValue = Amazon.DynamoDBv2.Model.AttributeValue;
using EfficientPutItemRequest = EfficientDynamoDb.Operations.PutItem.PutItemRequest;
using EfficientPutItemResponse = EfficientDynamoDb.Operations.PutItem.PutItemResponse;

namespace Goa.Clients.Dynamo.Benchmarks.Benchmarks;

[MemoryDiagnoser, GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory), Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class PutItemAsyncBenchmarks
{
    private LocalStackFixture _fixture = null!;
    private int _counter;

    [GlobalSetup]
    public void Setup()
    {
        _fixture = new LocalStackFixture();
        _fixture.StartAsync().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _fixture.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Put Item Simple")]
    public async Task<Amazon.DynamoDBv2.Model.PutItemResponse> AwsSdk_PutItem_Simple()
    {
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.PutItemAsync(new Amazon.DynamoDBv2.Model.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, AwsAttributeValue>
            {
                ["pk"] = new AwsAttributeValue($"aws-put-{id}"),
                ["sk"] = new AwsAttributeValue("simple"),
                ["data"] = new AwsAttributeValue("test-value"),
                ["number"] = new AwsAttributeValue { N = "42" }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Put Item Simple")]
    public async Task<bool> Goa_PutItem_Simple()
    {
        var id = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.PutItemAsync(new Goa.Clients.Dynamo.Operations.PutItem.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String($"goa-put-{id}"),
                ["sk"] = GoaModels.AttributeValue.String("simple"),
                ["data"] = GoaModels.AttributeValue.String("test-value"),
                ["number"] = GoaModels.AttributeValue.Number("42")
            }
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Put Item Simple")]
    public async Task<EfficientPutItemResponse> Efficient_PutItem_Simple()
    {
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.EfficientClient.PutItemAsync(new EfficientPutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Document
            {
                ["pk"] = $"eff-put-{id}",
                ["sk"] = "simple",
                ["data"] = "test-value",
                ["number"] = 42
            }
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Put Item Wide")]
    public async Task<Amazon.DynamoDBv2.Model.PutItemResponse> AwsSdk_PutItem_WideItem()
    {
        var id = Interlocked.Increment(ref _counter);
        var item = new Dictionary<string, AwsAttributeValue>
        {
            ["pk"] = new AwsAttributeValue($"aws-wide-{id}"),
            ["sk"] = new AwsAttributeValue("wide")
        };
        for (var i = 0; i < 20; i++)
        {
            item[$"attr_{i}"] = new AwsAttributeValue($"value-{i}-{new string('x', 100)}");
        }
        return await _fixture.AwsSdkClient.PutItemAsync(new Amazon.DynamoDBv2.Model.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = item
        });
    }

    [Benchmark, BenchmarkCategory("Put Item Wide")]
    public async Task<bool> Goa_PutItem_WideItem()
    {
        var id = Interlocked.Increment(ref _counter);
        var item = new Dictionary<string, GoaModels.AttributeValue>
        {
            ["pk"] = GoaModels.AttributeValue.String($"goa-wide-{id}"),
            ["sk"] = GoaModels.AttributeValue.String("wide")
        };
        for (var i = 0; i < 20; i++)
        {
            item[$"attr_{i}"] = GoaModels.AttributeValue.String($"value-{i}-{new string('x', 100)}");
        }
        var response = await _fixture.GoaClient.PutItemAsync(new Goa.Clients.Dynamo.Operations.PutItem.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = item
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Put Item Wide")]
    public async Task<EfficientPutItemResponse> Efficient_PutItem_WideItem()
    {
        var id = Interlocked.Increment(ref _counter);
        var item = new Document
        {
            ["pk"] = $"eff-wide-{id}",
            ["sk"] = "wide"
        };
        for (var i = 0; i < 20; i++)
        {
            item[$"attr_{i}"] = $"value-{i}-{new string('x', 100)}";
        }
        return await _fixture.EfficientClient.PutItemAsync(new EfficientPutItemRequest
        {
            TableName = _fixture.TableName,
            Item = item
        });
    }

    [Benchmark(Baseline = true), BenchmarkCategory("Put Item Complex")]
    public async Task<Amazon.DynamoDBv2.Model.PutItemResponse> AwsSdk_PutItem_ComplexItem()
    {
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.AwsSdkClient.PutItemAsync(new Amazon.DynamoDBv2.Model.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, AwsAttributeValue>
            {
                ["pk"] = new AwsAttributeValue($"aws-complex-{id}"),
                ["sk"] = new AwsAttributeValue("complex"),
                ["data"] = new AwsAttributeValue("test"),
                ["tags"] = new AwsAttributeValue { SS = ["tag1", "tag2", "tag3"] },
                ["scores"] = new AwsAttributeValue { NS = ["1", "2", "3"] },
                ["metadata"] = new AwsAttributeValue
                {
                    M = new Dictionary<string, AwsAttributeValue>
                    {
                        ["nested1"] = new AwsAttributeValue("val1"),
                        ["nested2"] = new AwsAttributeValue { N = "99" }
                    }
                },
                ["items"] = new AwsAttributeValue
                {
                    L =
                    [
                        new AwsAttributeValue("listval1"),
                        new AwsAttributeValue { N = "42" },
                        new AwsAttributeValue { BOOL = true }
                    ]
                }
            }
        });
    }

    [Benchmark, BenchmarkCategory("Put Item Complex")]
    public async Task<bool> Goa_PutItem_ComplexItem()
    {
        var id = Interlocked.Increment(ref _counter);
        var response = await _fixture.GoaClient.PutItemAsync(new Goa.Clients.Dynamo.Operations.PutItem.PutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Dictionary<string, GoaModels.AttributeValue>
            {
                ["pk"] = GoaModels.AttributeValue.String($"goa-complex-{id}"),
                ["sk"] = GoaModels.AttributeValue.String("complex"),
                ["data"] = GoaModels.AttributeValue.String("test"),
                ["tags"] = GoaModels.AttributeValue.FromStringSet(["tag1", "tag2", "tag3"]),
                ["scores"] = GoaModels.AttributeValue.FromNumberSet(["1", "2", "3"]),
                ["metadata"] = GoaModels.AttributeValue.FromMap(new Dictionary<string, GoaModels.AttributeValue>
                {
                    ["nested1"] = GoaModels.AttributeValue.String("val1"),
                    ["nested2"] = GoaModels.AttributeValue.Number("99")
                }),
                ["items"] = GoaModels.AttributeValue.FromList(
                [
                    GoaModels.AttributeValue.String("listval1"),
                    GoaModels.AttributeValue.Number("42"),
                    GoaModels.AttributeValue.Bool(true)
                ])
            }
        });
        return !response.IsError;
    }

    [Benchmark, BenchmarkCategory("Put Item Complex")]
    public async Task<EfficientPutItemResponse> Efficient_PutItem_ComplexItem()
    {
        var id = Interlocked.Increment(ref _counter);
        return await _fixture.EfficientClient.PutItemAsync(new EfficientPutItemRequest
        {
            TableName = _fixture.TableName,
            Item = new Document
            {
                ["pk"] = $"eff-complex-{id}",
                ["sk"] = "complex",
                ["data"] = "test",
                ["tags"] = new StringSetAttributeValue(new HashSet<string> { "tag1", "tag2", "tag3" }),
                ["scores"] = new NumberSetAttributeValue(new HashSet<string> { "1", "2", "3" }),
                ["metadata"] = new Document
                {
                    ["nested1"] = "val1",
                    ["nested2"] = new NumberAttributeValue("99")
                },
                ["items"] = new ListAttributeValue(new List<AttributeValue>
                {
                    new StringAttributeValue("listval1"),
                    new NumberAttributeValue("42"),
                    new BoolAttributeValue(true)
                })
            }
        });
    }
}
