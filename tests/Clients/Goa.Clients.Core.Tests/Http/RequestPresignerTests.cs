using ErrorOr;
using Goa.Clients.Core.Credentials;
using Goa.Clients.Core.Http;
using Moq;

namespace Goa.Clients.Core.Tests.Http;

/// <summary>
/// Verifies <see cref="RequestPresigner"/> against the AWS-documented SigV4 query-string example
/// (GET examplebucket/test.txt, credentials AKIAIOSFODNN7EXAMPLE, 20130524T000000Z) whose expected
/// signature is fixed and published, plus the session-token and extra-query-param behaviours.
/// </summary>
public class RequestPresignerTests
{
    private const string ExampleAccessKey = "AKIAIOSFODNN7EXAMPLE";
    private const string ExampleSecretKey = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY";
    private const string ExampleSignature = "aeeed9bbccd4d02ee5c0109b86d86835f995330da4c265957d157751f604d404";

    private static readonly DateTimeOffset ExampleTime = new(2013, 5, 24, 0, 0, 0, TimeSpan.Zero);

    private static RequestPresigner CreatePresigner(AwsCredentials credentials, DateTimeOffset time)
    {
        var chain = new Mock<ICredentialProviderChain>();
        chain.Setup(x => x.GetCredentialsAsync())
            .Returns(new ValueTask<ErrorOr<AwsCredentials>>(credentials));
        return new RequestPresigner(chain.Object, new FixedTimeProvider(time));
    }

    [Test]
    public async Task PresignAsync_MatchesAwsDocumentedExampleSignature()
    {
        var presigner = CreatePresigner(new AwsCredentials(ExampleAccessKey, ExampleSecretKey), ExampleTime);

        var result = await presigner.PresignAsync(new PresignParameters
        {
            Method = HttpMethod.Get,
            Uri = new Uri("https://examplebucket.s3.amazonaws.com/test.txt"),
            Region = "us-east-1",
            Service = "s3",
            Expiry = TimeSpan.FromSeconds(86400)
        });

        await Assert.That(result.IsError).IsFalse();
        var url = result.Value;
        await Assert.That(url).Contains($"X-Amz-Signature={ExampleSignature}");
        await Assert.That(url).StartsWith("https://examplebucket.s3.amazonaws.com/test.txt?");
        await Assert.That(url).Contains("X-Amz-Algorithm=AWS4-HMAC-SHA256");
        await Assert.That(url).Contains("X-Amz-Credential=AKIAIOSFODNN7EXAMPLE%2F20130524%2Fus-east-1%2Fs3%2Faws4_request");
        await Assert.That(url).Contains("X-Amz-Expires=86400");
        await Assert.That(url).Contains("X-Amz-SignedHeaders=host");
    }

    [Test]
    public async Task PresignAsync_WithSessionToken_IncludesEncodedSecurityTokenInQuery()
    {
        var presigner = CreatePresigner(
            new AwsCredentials(ExampleAccessKey, ExampleSecretKey, "tok/en+with=special"),
            ExampleTime);

        var result = await presigner.PresignAsync(new PresignParameters
        {
            Method = HttpMethod.Get,
            Uri = new Uri("https://examplebucket.s3.amazonaws.com/test.txt"),
            Region = "us-east-1",
            Service = "s3",
            Expiry = TimeSpan.FromSeconds(86400)
        });

        await Assert.That(result.IsError).IsFalse();
        // The session token is included and RFC 3986 encoded (/, +, = escaped).
        await Assert.That(result.Value).Contains("X-Amz-Security-Token=tok%2Fen%2Bwith%3Dspecial");
    }

    [Test]
    public async Task PresignAsync_WithResponseContentDisposition_SignsAndEncodesExtraQueryParam()
    {
        var presigner = CreatePresigner(new AwsCredentials(ExampleAccessKey, ExampleSecretKey), ExampleTime);

        var result = await presigner.PresignAsync(new PresignParameters
        {
            Method = HttpMethod.Get,
            Uri = new Uri("https://examplebucket.s3.amazonaws.com/test.txt"),
            Region = "us-east-1",
            Service = "s3",
            Expiry = TimeSpan.FromSeconds(60),
            QueryParameters = [new KeyValuePair<string, string>("response-content-disposition", "attachment; filename=\"a b.pdf\"")]
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value).Contains("response-content-disposition=attachment%3B%20filename%3D%22a%20b.pdf%22");
    }

    [Test]
    public async Task PresignAsync_ClampsExpiryToSevenDays()
    {
        var presigner = CreatePresigner(new AwsCredentials(ExampleAccessKey, ExampleSecretKey), ExampleTime);

        var result = await presigner.PresignAsync(new PresignParameters
        {
            Method = HttpMethod.Put,
            Uri = new Uri("https://examplebucket.s3.amazonaws.com/test.txt"),
            Region = "us-east-1",
            Service = "s3",
            Expiry = TimeSpan.FromDays(30)
        });

        await Assert.That(result.IsError).IsFalse();
        await Assert.That(result.Value).Contains("X-Amz-Expires=604800");
    }

    [Test]
    public async Task PresignAsync_CollapsesInternalWhitespaceInSignedHeaderValue()
    {
        // A single space and a double space in a signed header must produce the same signature,
        // because S3 collapses internal whitespace before it recomputes the signature.
        var single = await CreatePresigner(new AwsCredentials(ExampleAccessKey, ExampleSecretKey), ExampleTime)
            .PresignAsync(HeaderParams("a b"));
        var doubled = await CreatePresigner(new AwsCredentials(ExampleAccessKey, ExampleSecretKey), ExampleTime)
            .PresignAsync(HeaderParams("a  b"));

        await Assert.That(single.IsError).IsFalse();
        await Assert.That(doubled.IsError).IsFalse();
        await Assert.That(Signature(single.Value)).IsEqualTo(Signature(doubled.Value));

        static PresignParameters HeaderParams(string value) => new()
        {
            Method = HttpMethod.Put,
            Uri = new Uri("https://examplebucket.s3.amazonaws.com/test.txt"),
            Region = "us-east-1",
            Service = "s3",
            Expiry = TimeSpan.FromSeconds(60),
            SignedHeaders = [new KeyValuePair<string, string>("x-custom", value)]
        };

        static string Signature(string url) => url[(url.IndexOf("X-Amz-Signature=", StringComparison.Ordinal))..];
    }

    [Test]
    public async Task PresignAsync_WithCallerSuppliedReservedQueryParameter_ReturnsError()
    {
        var result = await CreatePresigner(new AwsCredentials(ExampleAccessKey, ExampleSecretKey), ExampleTime)
            .PresignAsync(new PresignParameters
            {
                Method = HttpMethod.Get,
                Uri = new Uri("https://examplebucket.s3.amazonaws.com/test.txt"),
                Region = "us-east-1",
                Service = "s3",
                Expiry = TimeSpan.FromSeconds(60),
                QueryParameters = [new KeyValuePair<string, string>("X-Amz-Expires", "1")]
            });

        await Assert.That(result.IsError).IsTrue();
    }

    [Test]
    public async Task PresignAsync_WithCallerSuppliedHostHeader_ReturnsError()
    {
        var result = await CreatePresigner(new AwsCredentials(ExampleAccessKey, ExampleSecretKey), ExampleTime)
            .PresignAsync(new PresignParameters
            {
                Method = HttpMethod.Put,
                Uri = new Uri("https://examplebucket.s3.amazonaws.com/test.txt"),
                Region = "us-east-1",
                Service = "s3",
                Expiry = TimeSpan.FromSeconds(60),
                SignedHeaders = [new KeyValuePair<string, string>("Host", "evil.example.com")]
            });

        await Assert.That(result.IsError).IsTrue();
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedTimeProvider(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
