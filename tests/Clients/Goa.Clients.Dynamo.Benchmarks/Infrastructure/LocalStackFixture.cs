using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using EfficientDynamoDb;
using EfficientDynamoDb.Configs;
using Goa.Clients.Core.Configuration;
using Goa.Clients.Dynamo;

namespace Goa.Clients.Dynamo.Benchmarks.Infrastructure;

public class LocalStackFixture : IAsyncDisposable
{
    private IContainer? _container;
    private ServiceProvider? _serviceProvider;

    public IDynamoClient GoaClient { get; private set; } = null!;
    public AmazonDynamoDBClient AwsSdkClient { get; private set; } = null!;
    public IDynamoDbLowLevelContext EfficientClient { get; private set; } = null!;
    public DynamoDbContext EfficientContext { get; private set; } = null!;
    public string TableName { get; } = "benchmark-table";
    public string ServiceUrl { get; private set; } = string.Empty;

    public async Task StartAsync()
    {
        _container = new ContainerBuilder(new DockerImage("localstack/localstack"))
            .WithEnvironment("SERVICES", "dynamodb")
            .WithEnvironment("LOCALSTACK_HOST", "localhost")
            .WithPortBinding(4566, true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(r => r.ForPort(4566).ForPath("/_localstack/health")))
            .Build();

        await _container.StartAsync();

        ServiceUrl = $"http://localhost:{_container.GetMappedPublicPort(4566)}";

        AwsSdkClient = new AmazonDynamoDBClient(
            new BasicAWSCredentials("test", "test"),
            new AmazonDynamoDBConfig
            {
                ServiceURL = ServiceUrl,
                AuthenticationRegion = "us-east-1"
            });

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHttpClient();
        services.AddStaticCredentials("test", "test");
        services.AddDynamoDB(config =>
        {
            config.ServiceUrl = ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        GoaClient = _serviceProvider.GetRequiredService<IDynamoClient>();

        // Setup EfficientDynamoDb client
        var region = RegionEndpoint.Create("us-east-1", ServiceUrl);
        var credentials = new AwsCredentials("test", "test");
        var efficientConfig = new DynamoDbContextConfig(region, credentials);
        EfficientContext = new DynamoDbContext(efficientConfig);
        EfficientClient = EfficientContext.LowLevel;

        await CreateTableAsync();
    }

    private async Task CreateTableAsync()
    {
        await AwsSdkClient.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
            KeySchema =
            [
                new KeySchemaElement("pk", KeyType.HASH),
                new KeySchemaElement("sk", KeyType.RANGE)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("pk", ScalarAttributeType.S),
                new AttributeDefinition("sk", ScalarAttributeType.S)
            ],
            BillingMode = BillingMode.PAY_PER_REQUEST
        });

        // Wait for table to be active
        for (var i = 0; i < 10; i++)
        {
            var response = await AwsSdkClient.DescribeTableAsync(new DescribeTableRequest
            {
                TableName = TableName
            });

            if (response.Table.TableStatus == TableStatus.ACTIVE) return;

            await Task.Delay(1000);
        }

        throw new TimeoutException($"DynamoDB table '{TableName}' did not reach Active state within 10 seconds.");
    }

    public async Task SeedItemsAsync(string pk, int count)
    {
        var items = new List<WriteRequest>();
        for (var i = 0; i < count; i++)
        {
            items.Add(new WriteRequest(new PutRequest(new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
            {
                ["pk"] = new(pk),
                ["sk"] = new($"item-{i:D4}"),
                ["data"] = new($"value-{i}"),
                ["number"] = new() { N = i.ToString() },
                ["status"] = new("active")
            })));

            if (items.Count == 25 || i == count - 1)
            {
                var requestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [TableName] = new(items)
                };

                var delay = 50;
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var response = await AwsSdkClient.BatchWriteItemAsync(new BatchWriteItemRequest
                    {
                        RequestItems = requestItems
                    });

                    if (response.UnprocessedItems == null || response.UnprocessedItems.Count == 0)
                        break;

                    requestItems = response.UnprocessedItems;
                    await Task.Delay(delay);
                    delay *= 2;
                }

                items.Clear();
            }
        }
    }

    public async Task SeedItemsByPkPrefixAsync(string pkPrefix, string sk, int count)
    {
        var items = new List<WriteRequest>();
        for (var i = 1; i <= count; i++)
        {
            items.Add(new WriteRequest(new PutRequest(new Dictionary<string, Amazon.DynamoDBv2.Model.AttributeValue>
            {
                ["pk"] = new($"{pkPrefix}{i}"),
                ["sk"] = new(sk),
                ["data"] = new($"value-{i}")
            })));

            if (items.Count == 25 || i == count)
            {
                var requestItems = new Dictionary<string, List<WriteRequest>>
                {
                    [TableName] = new(items)
                };

                var delay = 50;
                for (var attempt = 0; attempt < 5; attempt++)
                {
                    var response = await AwsSdkClient.BatchWriteItemAsync(new BatchWriteItemRequest
                    {
                        RequestItems = requestItems
                    });

                    if (response.UnprocessedItems == null || response.UnprocessedItems.Count == 0)
                        break;

                    requestItems = response.UnprocessedItems;
                    await Task.Delay(delay);
                    delay *= 2;
                }

                items.Clear();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        (EfficientContext as IDisposable)?.Dispose();
        AwsSdkClient?.Dispose();
        _serviceProvider?.Dispose();

        if (_container != null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }
}
