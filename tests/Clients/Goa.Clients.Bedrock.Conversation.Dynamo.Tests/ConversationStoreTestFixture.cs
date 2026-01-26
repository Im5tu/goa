using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Goa.Clients.Core.Configuration;
using Goa.Clients.Dynamo;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Goa.Clients.Bedrock.Conversation.Dynamo.Tests;

public class ConversationStoreTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private LocalStackFixture _localStack = null!;
    private ServiceProvider _serviceProvider = null!;

    public IConversationStore ConversationStore { get; private set; } = null!;
    public string TestTableName { get; } = $"conversations-{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        _localStack = new LocalStackFixture();
        await _localStack.InitializeAsync();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStaticCredentials("test", "test");
        services.AddDynamoDB(config =>
        {
            config.ServiceUrl = _localStack.ServiceUrl;
            config.Region = "us-east-1";
        });
        services.AddBedrockDynamoConversationStore(config =>
        {
            config.TableName = TestTableName;
        });

        _serviceProvider = services.BuildServiceProvider();
        ConversationStore = _serviceProvider.GetRequiredService<IConversationStore>();

        await CreateTestTableAsync();
    }

    private async Task CreateTestTableAsync()
    {
        var tempClient = new AmazonDynamoDBClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonDynamoDBConfig
        {
            ServiceURL = _localStack.ServiceUrl,
            UseHttp = true
        });

        var createTableRequest = new CreateTableRequest
        {
            TableName = TestTableName,
            KeySchema =
            [
                new KeySchemaElement("PK", KeyType.HASH),
                new KeySchemaElement("SK", KeyType.RANGE)
            ],
            AttributeDefinitions =
            [
                new AttributeDefinition("PK", ScalarAttributeType.S),
                new AttributeDefinition("SK", ScalarAttributeType.S)
            ],
            BillingMode = BillingMode.PAY_PER_REQUEST
        };

        try
        {
            await tempClient.CreateTableAsync(createTableRequest);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create test table: {ex.Message}", ex);
        }
        finally
        {
            tempClient.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var tempClient = new AmazonDynamoDBClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonDynamoDBConfig
            {
                ServiceURL = _localStack.ServiceUrl,
                UseHttp = true
            });

            try
            {
                await tempClient.DeleteTableAsync(TestTableName);
            }
            finally
            {
                tempClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to delete test table: {ex.Message}");
        }

        try
        {
            await _localStack.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to dispose LocalStack: {ex.Message}");
        }
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}