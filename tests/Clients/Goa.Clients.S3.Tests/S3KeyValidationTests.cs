using ErrorOr;
using Goa.Clients.Core.Configuration;
using Goa.Clients.S3.Operations.DeleteObject;
using Goa.Clients.S3.Operations.GetObject;
using Goa.Clients.S3.Operations.HeadObject;
using Goa.Clients.S3.Operations.PutObject;
using Microsoft.Extensions.DependencyInjection;

namespace Goa.Clients.S3.Tests;

public class S3KeyValidationTests
{
    private const string InvalidKeyErrorCode = "S3.InvalidKey";

    // Kept alive for the lifetime of the test class - the client resolves an IHttpClientFactory from
    // this provider, so it must not be disposed until all tests have run (see DisposeProvider).
    private static readonly ServiceProvider Provider = BuildProvider();

    // Points at an unroutable endpoint - invalid keys must be rejected before any request is sent.
    private static readonly IS3Client Client = Provider.GetRequiredService<IS3Client>();

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddStaticCredentials("test", "test");
        services.AddS3(config =>
        {
            config.ServiceUrl = "http://127.0.0.1:1";
            config.Region = "us-east-1";
        });

        return services.BuildServiceProvider();
    }

    [After(Class)]
    public static async Task DisposeProvider() => await Provider.DisposeAsync();

    [Test]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("../etc/passwd")]
    [Arguments("foo/../bar")]
    [Arguments("foo/./bar")]
    [Arguments("foo/..")]
    [Arguments("/foo")]
    [Arguments("foo//bar")]
    [Arguments("foo/")]
    public async Task PutObjectAsync_WithTraversalKey_ShouldReturnValidationError(string key)
    {
        // Act
        var result = await Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = "test-bucket",
            Key = key,
            Body = new byte[] { 1 }
        });

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(result.FirstError.Code).IsEqualTo(InvalidKeyErrorCode);
    }

    [Test]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("../etc/passwd")]
    [Arguments("foo/../bar")]
    [Arguments("foo/./bar")]
    [Arguments("foo/..")]
    [Arguments("/foo")]
    [Arguments("foo//bar")]
    [Arguments("foo/")]
    public async Task GetObjectAsync_WithTraversalKey_ShouldReturnValidationError(string key)
    {
        // Act
        var result = await Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = "test-bucket",
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(result.FirstError.Code).IsEqualTo(InvalidKeyErrorCode);
    }

    [Test]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("../etc/passwd")]
    [Arguments("foo/../bar")]
    [Arguments("foo/./bar")]
    [Arguments("foo/..")]
    [Arguments("/foo")]
    [Arguments("foo//bar")]
    [Arguments("foo/")]
    public async Task HeadObjectAsync_WithTraversalKey_ShouldReturnValidationError(string key)
    {
        // Act
        var result = await Client.HeadObjectAsync(new HeadObjectRequest
        {
            Bucket = "test-bucket",
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(result.FirstError.Code).IsEqualTo(InvalidKeyErrorCode);
    }

    [Test]
    [Arguments(".")]
    [Arguments("..")]
    [Arguments("../etc/passwd")]
    [Arguments("foo/../bar")]
    [Arguments("foo/./bar")]
    [Arguments("foo/..")]
    [Arguments("/foo")]
    [Arguments("foo//bar")]
    [Arguments("foo/")]
    public async Task DeleteObjectAsync_WithTraversalKey_ShouldReturnValidationError(string key)
    {
        // Act
        var result = await Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            Bucket = "test-bucket",
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(result.FirstError.Code).IsEqualTo(InvalidKeyErrorCode);
    }

    [Test]
    [Arguments(".hidden")]
    [Arguments("..config")]
    [Arguments("file.name.txt")]
    [Arguments("v1.0/archive..tar.gz")]
    [Arguments("a/b.c/d")]
    public async Task GetObjectAsync_WithDottedButValidKey_ShouldNotReturnInvalidKeyError(string key)
    {
        // Act - the unroutable endpoint means a key that passes validation fails at transport instead
        var result = await Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = "test-bucket",
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsNotEqualTo(InvalidKeyErrorCode);
        await Assert.That(result.FirstError.Type).IsNotEqualTo(ErrorType.Validation);
    }
}
