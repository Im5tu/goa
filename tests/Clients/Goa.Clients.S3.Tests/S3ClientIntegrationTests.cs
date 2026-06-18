using ErrorOr;
using Goa.Clients.S3.Operations.DeleteObject;
using Goa.Clients.S3.Operations.GetObject;
using Goa.Clients.S3.Operations.HeadObject;
using Goa.Clients.S3.Operations.PutObject;
using System.Text;

namespace Goa.Clients.S3.Tests;

[ClassDataSource<S3TestFixture>(Shared = SharedType.PerAssembly)]
public class S3ClientIntegrationTests
{
    private readonly S3TestFixture _fixture;

    public S3ClientIntegrationTests(S3TestFixture fixture)
    {
        _fixture = fixture;
    }

    private static string NewKey() => $"{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}";

    [Test]
    public async Task PutObjectAsync_WithValidRequest_ShouldReturnETag()
    {
        // Arrange
        var request = new PutObjectBuilder()
            .WithBucket(_fixture.BucketName)
            .WithKey(NewKey())
            .WithBody(Encoding.UTF8.GetBytes("Hello, S3!"))
            .WithContentType("text/plain")
            .Build();

        // Act
        var result = await _fixture.S3Client.PutObjectAsync(request);

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ETag).IsNotNull();
    }

    [Test]
    public async Task GetObjectAsync_AfterPut_ShouldRoundTripBodyAndContentType()
    {
        // Arrange
        var key = NewKey();
        var body = Encoding.UTF8.GetBytes("{\"message\":\"round trip\"}");

        var putResult = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = body,
            ContentType = "application/json"
        });

        await Assert.That(putResult.IsError).IsFalse();

        // Act
        var result = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Body).IsEquivalentTo(body);
        await Assert.That(result.Value.ContentType).IsEqualTo("application/json");
        await Assert.That(result.Value.ContentLength).IsEqualTo(body.Length);
        await Assert.That(result.Value.ETag).IsNotNull();
    }

    [Test]
    public async Task HeadObjectAsync_AfterPut_ShouldReturnSizeAndETag()
    {
        // Arrange
        var key = NewKey();
        var body = Encoding.UTF8.GetBytes("head object content");

        var putResult = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = body,
            ContentType = "text/plain"
        });

        await Assert.That(putResult.IsError).IsFalse();

        // Act
        var result = await _fixture.S3Client.HeadObjectAsync(new HeadObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ContentLength).IsEqualTo(body.Length);
        await Assert.That(result.Value.ETag).IsEqualTo(putResult.Value.ETag);
        await Assert.That(result.Value.LastModified).IsNotNull();
    }

    [Test]
    public async Task GetObjectAsync_WithRange_ShouldReturnExactSlice()
    {
        // Arrange
        var key = NewKey();
        var body = new byte[256];
        for (var i = 0; i < body.Length; i++)
            body[i] = (byte)i;

        var putResult = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = body
        });

        await Assert.That(putResult.IsError).IsFalse();

        // Act
        var result = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Range = "bytes=0-63"
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Body).IsEquivalentTo(body.AsSpan(0, 64).ToArray());
    }

    [Test]
    public async Task GetObjectAsync_WithMissingKey_ShouldReturnNotFound()
    {
        // Act
        var result = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = NewKey()
        });

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task HeadObjectAsync_WithMissingKey_ShouldReturnNotFound()
    {
        // Act
        var result = await _fixture.S3Client.HeadObjectAsync(new HeadObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = NewKey()
        });

        // Assert
        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task DeleteObjectAsync_WithExistingKey_ShouldSucceed()
    {
        // Arrange
        var key = NewKey();

        var putResult = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = Encoding.UTF8.GetBytes("delete me")
        });

        await Assert.That(putResult.IsError).IsFalse();

        // Act
        var result = await _fixture.S3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();

        var getResult = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        await Assert.That(getResult.IsError).IsTrue();
        await Assert.That(getResult.FirstError.Type).IsEqualTo(ErrorType.NotFound);
    }

    [Test]
    public async Task DeleteObjectAsync_WithMissingKey_ShouldSucceed()
    {
        // Act - S3 returns 204 even when the key does not exist on an unversioned bucket
        var result = await _fixture.S3Client.DeleteObjectAsync(new DeleteObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = NewKey()
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();
    }

    [Test]
    public async Task PutObjectAsync_With4MBBody_ShouldRoundTrip()
    {
        // Arrange
        var key = NewKey();
        var body = new byte[4 * 1024 * 1024];
        Random.Shared.NextBytes(body);

        var putResult = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = body,
            ContentType = "application/octet-stream"
        });

        await Assert.That(putResult.IsError).IsFalse();

        // Act
        var result = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ContentLength).IsEqualTo(body.Length);
        await Assert.That(result.Value.Body).IsEquivalentTo(body);
    }

    [Test]
    public async Task PutObjectAsync_WithMetadata_ShouldRoundTripMetadata()
    {
        // Arrange
        var key = NewKey();
        var request = new PutObjectBuilder()
            .WithBucket(_fixture.BucketName)
            .WithKey(key)
            .WithBody(Encoding.UTF8.GetBytes("metadata content"))
            .AddMetadata("category", "test")
            .AddMetadata("origin", "integration")
            .Build();

        var putResult = await _fixture.S3Client.PutObjectAsync(request);
        await Assert.That(putResult.IsError).IsFalse();

        // Act
        var result = await _fixture.S3Client.HeadObjectAsync(new HeadObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Metadata).IsNotNull();
        await Assert.That(result.Value.Metadata!["category"]).IsEqualTo("test");
        await Assert.That(result.Value.Metadata!["origin"]).IsEqualTo("integration");
    }

    [Test]
    [Arguments("folder name/file+v1 (final).txt")]
    [Arguments("emoji/日本語-ключ-clé.txt")]
    public async Task PutAndGet_WithKeyRequiringPercentEncoding_ShouldRoundTrip(string keySuffix)
    {
        // Exercises the %XX key-encoding path end-to-end with signature validation ON.
        // The unique prefix avoids collisions while keeping the encoded segment intact.
        var key = $"{Guid.NewGuid():N}/{keySuffix}";
        var body = Encoding.UTF8.GetBytes("encoded key round trip");

        var putResult = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = body,
            ContentType = "text/plain"
        });

        await Assert.That(putResult.IsError).IsFalse();

        var result = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Body).IsEquivalentTo(body);
    }

    [Test]
    public async Task PutObjectAsync_WithZeroByteBody_ShouldRoundTrip()
    {
        // Exercises the empty-payload-hash path with signature validation ON.
        var key = NewKey();

        var putResult = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = ReadOnlyMemory<byte>.Empty
        });

        await Assert.That(putResult.IsError).IsFalse();

        var result = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.ContentLength).IsEqualTo(0L);
        await Assert.That(result.Value.Body.Length).IsEqualTo(0);
    }

    [Test]
    [Arguments("../escape")]
    [Arguments("a/../b")]
    [Arguments("a/./b")]
    [Arguments("/leading")]
    [Arguments("a//b")]
    public async Task GetObjectAsync_WithDotSegmentKey_ShouldReturnValidationError_VirtualHost(string key)
    {
        var result = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
    }

    [Test]
    [Arguments("../escape")]
    [Arguments("a/../b")]
    [Arguments("a/./b")]
    [Arguments("/leading")]
    [Arguments("a//b")]
    public async Task DeleteObjectAsync_WithDotSegmentKey_ShouldReturnValidationError_PathStyle(string key)
    {
        var client = _fixture.CreatePathStyleClient();

        var result = await client.DeleteObjectAsync(new DeleteObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
    }

    [Test]
    public async Task PathStyleClient_PutAndGet_ShouldSucceed()
    {
        // Arrange
        var client = _fixture.CreatePathStyleClient();
        var key = NewKey();
        var body = Encoding.UTF8.GetBytes("path-style addressing");

        var putResult = await client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = body,
            ContentType = "text/plain"
        });

        await Assert.That(putResult.IsError).IsFalse();

        // Act
        var result = await client.GetObjectAsync(new GetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });

        // Assert
        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value.Body).IsEquivalentTo(body);
    }
}
