using Goa.Clients.S3.Operations.PresignGetObject;
using Goa.Clients.S3.Operations.PresignPutObject;
using Goa.Clients.S3.Operations.PutObject;
using System.Text;

namespace Goa.Clients.S3.Tests;

/// <summary>
/// End-to-end tests for pre-signed URL generation. Each test signs a URL with the Goa client, then
/// exercises it with a bare <see cref="HttpClient"/> (carrying no AWS credentials) against LocalStack,
/// proving the URL is structurally valid and usable for direct-to-S3 transfer.
/// Exact SigV4 signature correctness is asserted separately and deterministically in
/// <c>Goa.Clients.Core.Tests.Http.RequestPresignerTests</c>.
/// </summary>
[ClassDataSource<S3TestFixture>(Shared = SharedType.PerAssembly)]
public class S3PresignIntegrationTests
{
    private readonly S3TestFixture _fixture;

    public S3PresignIntegrationTests(S3TestFixture fixture)
    {
        _fixture = fixture;
    }

    private static string NewKey() => $"{Guid.NewGuid()}/{Guid.NewGuid()}/{Guid.NewGuid()}";

    [Test]
    public async Task PresignPut_ThenPresignGet_RoundTripsObjectBytes()
    {
        var key = NewKey();
        var body = Encoding.UTF8.GetBytes("pre-signed round trip payload");
        using var http = new HttpClient();

        // Upload directly to S3 via a pre-signed PUT URL.
        var putUrl = await _fixture.S3Client.PresignPutObjectAsync(new PresignPutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            ContentLength = body.Length
        });
        await Assert.That(putUrl.IsError).IsFalse();

        using var putResponse = await http.PutAsync(putUrl.Value, new ByteArrayContent(body));
        await Assert.That(putResponse.IsSuccessStatusCode).IsTrue();

        // Download directly from S3 via a pre-signed GET URL.
        var getUrl = await _fixture.S3Client.PresignGetObjectAsync(new PresignGetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key
        });
        await Assert.That(getUrl.IsError).IsFalse();

        using var getResponse = await http.GetAsync(getUrl.Value);
        await Assert.That(getResponse.IsSuccessStatusCode).IsTrue();
        var downloaded = await getResponse.Content.ReadAsByteArrayAsync();
        await Assert.That(downloaded).IsEquivalentTo(body);
    }

    [Test]
    public async Task PresignGet_WithResponseContentDisposition_OverridesDownloadHeader()
    {
        var key = NewKey();
        var body = Encoding.UTF8.GetBytes("attachment payload");

        var put = await _fixture.S3Client.PutObjectAsync(new PutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            Body = body,
            ContentType = "application/octet-stream"
        });
        await Assert.That(put.IsError).IsFalse();

        var getUrl = await _fixture.S3Client.PresignGetObjectAsync(new PresignGetObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = key,
            ResponseContentType = "application/pdf",
            ResponseContentDisposition = "attachment; filename=\"passport.pdf\""
        });
        await Assert.That(getUrl.IsError).IsFalse();

        using var http = new HttpClient();
        using var response = await http.GetAsync(getUrl.Value);
        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        await Assert.That(response.Content.Headers.ContentDisposition?.ToString())
            .IsEqualTo("attachment; filename=\"passport.pdf\"");
        await Assert.That(response.Content.Headers.ContentType?.ToString()).IsEqualTo("application/pdf");
    }

    [Test]
    public async Task PresignPut_WithDotSegmentKey_ReturnsValidationError()
    {
        var result = await _fixture.S3Client.PresignPutObjectAsync(new PresignPutObjectRequest
        {
            Bucket = _fixture.BucketName,
            Key = "../escape"
        });

        await Assert.That(result.IsError).IsTrue();
    }
}
