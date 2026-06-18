using ErrorOr;
using Goa.Clients.S3.Errors;

namespace Goa.Clients.S3;

/// <summary>
/// Shared validation for S3 bucket names and object keys.
/// Keys and bucket names are interpolated directly into the request URI, so they must be
/// validated before signing to prevent path traversal (dot-segments collapsing the path)
/// and malformed host names.
/// </summary>
internal static class S3RequestValidation
{
    /// <summary>
    /// Validates an object key. Rejects empty keys, keys whose '/'-split produces an empty,
    /// "." or ".." segment (these are normalized away by <see cref="Uri"/> and can escape the
    /// intended object, e.g. turning DeleteObject into DeleteBucket), and keys containing
    /// control characters.
    /// </summary>
    public static ErrorOr<Success> ValidateKey(string? key)
    {
        if (string.IsNullOrEmpty(key))
            return Error.Validation(S3ErrorCodes.InvalidKey, "Key is required.");

        var start = 0;
        for (var i = 0; i <= key.Length; i++)
        {
            if (i == key.Length || key[i] == '/')
            {
                var segLen = i - start;
                if (segLen == 0)
                    return Error.Validation(S3ErrorCodes.InvalidKey, "Key must not contain empty path segments.");
                if (segLen == 1 && key[start] == '.')
                    return Error.Validation(S3ErrorCodes.InvalidKey, "Key must not contain '.' path segments.");
                if (segLen == 2 && key[start] == '.' && key[start + 1] == '.')
                    return Error.Validation(S3ErrorCodes.InvalidKey, "Key must not contain '..' path segments.");
                start = i + 1;
            }
            else if (char.IsControl(key[i]))
            {
                return Error.Validation(S3ErrorCodes.InvalidKey, "Key must not contain control characters.");
            }
        }

        return Result.Success;
    }

    /// <summary>
    /// Validates a bucket name against S3 naming rules: 3-63 characters, only lowercase
    /// letters, digits, hyphens and dots, must start and end with a letter or digit, and
    /// must not contain consecutive dots.
    /// </summary>
    public static ErrorOr<Success> ValidateBucketName(string? bucket)
    {
        if (string.IsNullOrEmpty(bucket))
            return Error.Validation(S3ErrorCodes.InvalidBucketName, "Bucket is required.");

        if (bucket.Length is < 3 or > 63)
            return Error.Validation(S3ErrorCodes.InvalidBucketName, "Bucket name must be between 3 and 63 characters.");

        if (!IsBucketBoundaryChar(bucket[0]) || !IsBucketBoundaryChar(bucket[^1]))
            return Error.Validation(S3ErrorCodes.InvalidBucketName, "Bucket name must start and end with a lowercase letter or digit.");

        for (var i = 0; i < bucket.Length; i++)
        {
            var ch = bucket[i];
            if (!IsBucketChar(ch))
                return Error.Validation(S3ErrorCodes.InvalidBucketName, "Bucket name may only contain lowercase letters, digits, hyphens and dots.");

            if (ch == '.' && i + 1 < bucket.Length && bucket[i + 1] == '.')
                return Error.Validation(S3ErrorCodes.InvalidBucketName, "Bucket name must not contain consecutive dots.");
        }

        return Result.Success;
    }

    /// <summary>
    /// Validates user-defined metadata. Each name is sent as an "x-amz-meta-{name}" header, so it
    /// must be a valid HTTP token; invalid tokens would otherwise be silently dropped by the HTTP
    /// stack. This mirrors the validation enforced by <c>PutObjectBuilder.AddMetadata</c> so that
    /// directly-constructed requests are held to the same rules.
    /// </summary>
    public static ErrorOr<Success> ValidateMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
            return Result.Success;

        foreach (var entry in metadata)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || !IsValidHttpToken(entry.Key))
                return Error.Validation(
                    S3ErrorCodes.InvalidMetadata,
                    $"Metadata name '{entry.Key}' is not a valid HTTP token. Names must not contain spaces, colons, control or separator characters.");
        }

        return Result.Success;
    }

    /// <summary>
    /// Determines whether a string is a valid HTTP token per RFC 7230 (used for header names).
    /// Invalid tokens would be silently dropped by the HTTP stack, so they are rejected up-front.
    /// </summary>
    public static bool IsValidHttpToken(string value)
    {
        foreach (var ch in value)
        {
            if (ch <= ' ' || ch >= 0x7F)
                return false;

            switch (ch)
            {
                case '(' or ')' or '<' or '>' or '@'
                    or ',' or ';' or ':' or '\\' or '"'
                    or '/' or '[' or ']' or '?' or '='
                    or '{' or '}':
                    return false;
            }
        }

        return true;
    }

    private static bool IsBucketBoundaryChar(char ch) =>
        ch is (>= 'a' and <= 'z') or (>= '0' and <= '9');

    private static bool IsBucketChar(char ch) =>
        ch is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-' or '.';
}
