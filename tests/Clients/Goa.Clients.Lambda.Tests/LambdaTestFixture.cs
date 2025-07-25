using System.IO.Compression;
using System.Text;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TUnit.Core.Interfaces;

namespace Goa.Clients.Lambda.Tests;

public class LambdaTestFixture : IAsyncInitializer, IAsyncDisposable
{
    private LocalStackFixture _localStack = null!;
    private ServiceProvider _serviceProvider = null!;

    public ILambdaClient LambdaClient { get; private set; } = null!;
    public string TestFunctionName { get; } = $"test-function-{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        _localStack = new LocalStackFixture();
        await _localStack.InitializeAsync();

        var services = new ServiceCollection();

        services.AddLogging(b => b.AddConsole());
        services.AddStaticCredentials("test", "test");
        services.AddLambda(config =>
        {
            config.ServiceUrl = _localStack.ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        LambdaClient = _serviceProvider.GetRequiredService<ILambdaClient>();

        await CreateTestFunctionAsync();
    }

    private async Task CreateTestFunctionAsync()
    {
        var tempClient = new AmazonLambdaClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonLambdaConfig
        {
            ServiceURL = _localStack.ServiceUrl,
            UseHttp = true
        });

        // Create a simple Python function that echoes the input
        var functionCode = @"
def lambda_handler(event, context):
    return {
        'statusCode': 200,
        'body': event
    }
";

        var zipBytes = CreateZipFromString("lambda_function.py", functionCode);

        var createFunctionRequest = new CreateFunctionRequest
        {
            FunctionName = TestFunctionName,
            Runtime = Runtime.Python39,
            Role = "arn:aws:iam::123456789012:role/lambda-role",
            Handler = "lambda_function.lambda_handler",
            Code = new FunctionCode
            {
                ZipFile = new MemoryStream(zipBytes)
            },
            Description = "Test function for Goa Lambda client",
            Timeout = 30,
            Publish = true
        };

        try
        {
            await tempClient.CreateFunctionAsync(createFunctionRequest);
            var status = State.Pending;
            var counter = 10;
            do
            {
                status = (await tempClient.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest
                {
                    FunctionName = TestFunctionName,
                })).State;
                await Task.Delay(TimeSpan.FromSeconds(2));
            } while (status != State.Active && counter-- > 0);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create test Lambda function: {ex.Message}", ex);
        }
        finally
        {
            tempClient.Dispose();
        }
    }

    private static byte[] CreateZipFromString(string fileName, string content)
    {
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(fileName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write(content);
        }
        return memoryStream.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var tempClient = new AmazonLambdaClient(new BasicAWSCredentials("XXX", "XXX"), new AmazonLambdaConfig
            {
                ServiceURL = _localStack.ServiceUrl,
                UseHttp = true
            });

            try
            {
                await tempClient.DeleteFunctionAsync(new DeleteFunctionRequest { FunctionName = TestFunctionName });
            }
            finally
            {
                tempClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to delete test Lambda function: {ex.Message}");
        }

        if (_localStack != null)
            await _localStack.DisposeAsync();

        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
