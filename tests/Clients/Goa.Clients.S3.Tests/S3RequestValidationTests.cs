using ErrorOr;
using Goa.Clients.S3;
using Goa.Clients.S3.Errors;

namespace Goa.Clients.S3.Tests;

/// <summary>
/// Pure unit tests for bucket name and object key validation. These do not require LocalStack.
/// </summary>
public class S3RequestValidationTests
{
    [Test]
    [Arguments("../etc/passwd")]
    [Arguments("..")]
    [Arguments("a/../b")]
    [Arguments("a/./b")]
    [Arguments("./a")]
    [Arguments("a/.")]
    [Arguments("/leading")]
    [Arguments("a//b")]
    [Arguments("trailing/")]
    [Arguments("")]
    public async Task ValidateKey_WithDotSegmentOrEmptyKey_ReturnsValidationError(string key)
    {
        var result = S3RequestValidation.ValidateKey(key);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(result.FirstError.Code).IsEqualTo(S3ErrorCodes.InvalidKey);
    }

    [Test]
    public async Task ValidateKey_WithControlCharacter_ReturnsValidationError()
    {
        var key = "folder/name"; // embedded BEL control character

        var result = S3RequestValidation.ValidateKey(key);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Code).IsEqualTo(S3ErrorCodes.InvalidKey);
    }

    [Test]
    [Arguments("simple.txt")]
    [Arguments("folder/sub/object.json")]
    [Arguments("folder name/file+v1 (final).txt")]
    [Arguments("a..b/c")]
    [Arguments("...")]
    [Arguments("café/naïve.txt")]
    public async Task ValidateKey_WithValidKey_ReturnsSuccess(string key)
    {
        var result = S3RequestValidation.ValidateKey(key);

        await Assert.That(result.IsError).IsFalse();
    }

    [Test]
    [Arguments("ab")]
    [Arguments("this-bucket-name-is-far-too-long-to-be-valid-because-it-exceeds-63ch")]
    [Arguments("MyBucket")]
    [Arguments("bucket_name")]
    [Arguments("bucket name")]
    [Arguments("bucket/name")]
    [Arguments("-bucket")]
    [Arguments("bucket-")]
    [Arguments(".bucket")]
    [Arguments("bucket..name")]
    [Arguments("")]
    public async Task ValidateBucketName_WithInvalidName_ReturnsValidationError(string bucket)
    {
        var result = S3RequestValidation.ValidateBucketName(bucket);

        await Assert.That(result.IsError).IsTrue();
        await Assert.That(result.FirstError.Type).IsEqualTo(ErrorType.Validation);
        await Assert.That(result.FirstError.Code).IsEqualTo(S3ErrorCodes.InvalidBucketName);
    }

    [Test]
    [Arguments("my-bucket")]
    [Arguments("my.bucket.name")]
    [Arguments("bucket123")]
    [Arguments("123bucket")]
    public async Task ValidateBucketName_WithValidName_ReturnsSuccess(string bucket)
    {
        var result = S3RequestValidation.ValidateBucketName(bucket);

        await Assert.That(result.IsError).IsFalse();
    }
}
