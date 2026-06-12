using Goa.Clients.S3.Operations.PutObject;

namespace Goa.Clients.S3.Tests;

/// <summary>
/// Unit tests for <see cref="PutObjectBuilder"/>. These do not require LocalStack.
/// </summary>
public class PutObjectBuilderTests
{
    [Test]
    [Arguments("bad name")]   // space
    [Arguments("bad:name")]   // colon
    [Arguments("bad/name")]   // separator
    [Arguments("bad@name")]   // separator
    public async Task AddMetadata_WithInvalidHttpToken_Throws(string name)
    {
        var builder = new PutObjectBuilder();

        await Assert.That(() => builder.AddMetadata(name, "value"))
            .Throws<ArgumentException>();
    }

    [Test]
    [Arguments("category")]
    [Arguments("uploaded-by")]
    [Arguments("x-custom_token.1")]
    public async Task AddMetadata_WithValidHttpToken_Succeeds(string name)
    {
        var builder = new PutObjectBuilder();

        var result = builder.AddMetadata(name, "value");

        await Assert.That(result).IsSameReferenceAs(builder);
    }
}
