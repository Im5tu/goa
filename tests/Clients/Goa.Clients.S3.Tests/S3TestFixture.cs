using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Goa.Clients.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TUnit.Core.Interfaces;

namespace Goa.Clients.S3.Tests;

public class S3TestFixture : IAsyncInitializer, IAsyncDisposable
{
    private LocalStackFixture _localStack = null!;
    private ServiceProvider _serviceProvider = null!;
    private ServiceProvider _pathStyleServiceProvider = null!;
    private IS3Client _pathStyleClient = null!;

    public IS3Client S3Client { get; private set; } = null!;

    public string BucketName { get; } = $"s3-test-bucket-{Guid.NewGuid():N}";

    public async Task InitializeAsync()
    {
        _localStack = new LocalStackFixture();
        await _localStack.InitializeAsync();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStaticCredentials("test", "test");
        services.AddS3(config =>
        {
            config.ServiceUrl = _localStack.ServiceUrl;
            config.Region = "us-east-1";
        });

        _serviceProvider = services.BuildServiceProvider();
        S3Client = _serviceProvider.GetRequiredService<IS3Client>();

        // Eagerly build the path-style client during single-threaded initialization so
        // CreatePathStyleClient is safe to call from tests running in parallel under TUnit.
        var pathStyleServices = new ServiceCollection();
        pathStyleServices.AddLogging();
        pathStyleServices.AddStaticCredentials("test", "test");
        pathStyleServices.AddS3(config =>
        {
            config.ServiceUrl = _localStack.ServiceUrl;
            config.Region = "us-east-1";
            config.ForcePathStyle = true;
        });

        _pathStyleServiceProvider = pathStyleServices.BuildServiceProvider();
        _pathStyleClient = _pathStyleServiceProvider.GetRequiredService<IS3Client>();

        await CreateTestBucketAsync(BucketName);
    }

    public IS3Client CreatePathStyleClient() => _pathStyleClient;

    public async Task CreateTestBucketAsync(string bucketName)
    {
        var tempClient = new AmazonS3Client(new BasicAWSCredentials("XXX", "XXX"), new AmazonS3Config
        {
            ServiceURL = _localStack.ServiceUrl,
            UseHttp = true,
            ForcePathStyle = true
        });

        try
        {
            await tempClient.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create test S3 bucket: {ex.Message}", ex);
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
            var tempClient = new AmazonS3Client(new BasicAWSCredentials("XXX", "XXX"), new AmazonS3Config
            {
                ServiceURL = _localStack.ServiceUrl,
                UseHttp = true,
                ForcePathStyle = true
            });

            try
            {
                string? continuationToken = null;
                do
                {
                    var objects = await tempClient.ListObjectsV2Async(new ListObjectsV2Request
                    {
                        BucketName = BucketName,
                        ContinuationToken = continuationToken
                    });

                    foreach (var s3Object in objects.S3Objects ?? [])
                    {
                        await tempClient.DeleteObjectAsync(BucketName, s3Object.Key);
                    }

                    continuationToken = objects.IsTruncated == true ? objects.NextContinuationToken : null;
                }
                while (continuationToken is not null);

                await tempClient.DeleteBucketAsync(BucketName);
            }
            finally
            {
                tempClient.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to delete test S3 bucket: {ex.Message}");
        }

        if (_localStack != null)
            await _localStack.DisposeAsync();

        _serviceProvider?.Dispose();
        _pathStyleServiceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
