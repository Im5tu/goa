using ErrorOr;
using Goa.Clients.Core;
using Goa.Clients.Core.Http;
using Goa.Clients.Core.Logging;
using Goa.Clients.S3.Errors;
using Goa.Clients.S3.Operations.DeleteObject;
using Goa.Clients.S3.Operations.GetObject;
using Goa.Clients.S3.Operations.HeadObject;
using Goa.Clients.S3.Operations.PutObject;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Goa.Clients.S3;

internal sealed class S3ServiceClient : AwsServiceClient<S3ServiceClientConfiguration>, IS3Client
{
    private const string MetadataHeaderPrefix = "x-amz-meta-";

    private string? _cachedPathStyleBaseUrl;

    public S3ServiceClient(
        IHttpClientFactory httpClientFactory,
        S3ServiceClientConfiguration configuration,
        ILogger<S3ServiceClient> logger)
        : base(httpClientFactory, logger, configuration)
    {
    }

    public async Task<ErrorOr<PutObjectResponse>> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidateBucketAndKey(request.Bucket, request.Key);
        if (validation.IsError)
            return validation.Errors;

        try
        {
            using var requestMessage = CreateObjectRequest(HttpMethod.Put, request.Bucket, request.Key);

            var content = new ReadOnlyMemoryContent(request.Body);
            if (!string.IsNullOrWhiteSpace(request.ContentType))
                content.Headers.ContentType = MediaTypeHeaderValue.Parse(request.ContentType);
            requestMessage.Content = content;

            if (request.Body.Length > 0)
                requestMessage.Options.Set(HttpOptions.Payload, request.Body);

            if (!string.IsNullOrWhiteSpace(request.ServerSideEncryption))
                requestMessage.Headers.TryAddWithoutValidation("x-amz-server-side-encryption", request.ServerSideEncryption);

            if (!string.IsNullOrWhiteSpace(request.SseKmsKeyId))
                requestMessage.Headers.TryAddWithoutValidation("x-amz-server-side-encryption-aws-kms-key-id", request.SseKmsKeyId);

            if (request.Metadata is { Count: > 0 })
            {
                foreach (var entry in request.Metadata)
                    requestMessage.Headers.TryAddWithoutValidation(MetadataHeaderPrefix + entry.Key, entry.Value);
            }

            using var response = await SendAsync(requestMessage, "PutObject", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ReadErrorAsync(response, "PutObject", cancellationToken);

            return new PutObjectResponse
            {
                ETag = GetETag(response)
            };
        }
        catch (Exception ex)
        {
            Logger.PutObjectFailed(ex, request.Bucket, request.Key);
            return Error.Failure("S3.PutObject.Failed", $"Failed to put object {request.Key} to S3 bucket {request.Bucket}");
        }
    }

    public async Task<ErrorOr<GetObjectResponse>> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidateBucketAndKey(request.Bucket, request.Key);
        if (validation.IsError)
            return validation.Errors;

        try
        {
            using var requestMessage = CreateObjectRequest(HttpMethod.Get, request.Bucket, request.Key);

            if (!string.IsNullOrWhiteSpace(request.Range))
                requestMessage.Headers.TryAddWithoutValidation("Range", request.Range);

            using var response = await SendAsync(requestMessage, "GetObject", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ReadErrorAsync(response, "GetObject", cancellationToken);

            var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);

            return new GetObjectResponse
            {
                Body = body,
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength ?? body.Length,
                ETag = GetETag(response),
                LastModified = response.Content.Headers.LastModified,
                Metadata = ReadMetadata(response)
            };
        }
        catch (Exception ex)
        {
            Logger.GetObjectFailed(ex, request.Bucket, request.Key);
            return Error.Failure("S3.GetObject.Failed", $"Failed to get object {request.Key} from S3 bucket {request.Bucket}");
        }
    }

    public async Task<ErrorOr<HeadObjectResponse>> HeadObjectAsync(HeadObjectRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidateBucketAndKey(request.Bucket, request.Key);
        if (validation.IsError)
            return validation.Errors;

        try
        {
            using var requestMessage = CreateObjectRequest(HttpMethod.Head, request.Bucket, request.Key);
            using var response = await SendAsync(requestMessage, "HeadObject", cancellationToken);

            if (!response.IsSuccessStatusCode)
                return await ReadErrorAsync(response, "HeadObject", cancellationToken);

            return new HeadObjectResponse
            {
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength ?? 0,
                ETag = GetETag(response),
                LastModified = response.Content.Headers.LastModified,
                Metadata = ReadMetadata(response)
            };
        }
        catch (Exception ex)
        {
            Logger.HeadObjectFailed(ex, request.Bucket, request.Key);
            return Error.Failure("S3.HeadObject.Failed", $"Failed to head object {request.Key} in S3 bucket {request.Bucket}");
        }
    }

    public async Task<ErrorOr<DeleteObjectResponse>> DeleteObjectAsync(DeleteObjectRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validation = ValidateBucketAndKey(request.Bucket, request.Key);
        if (validation.IsError)
            return validation.Errors;

        try
        {
            using var requestMessage = CreateObjectRequest(HttpMethod.Delete, request.Bucket, request.Key);
            using var response = await SendAsync(requestMessage, "DeleteObject", cancellationToken);

            // S3 returns 204 No Content even when the key does not exist on an unversioned bucket.
            if (!response.IsSuccessStatusCode)
                return await ReadErrorAsync(response, "DeleteObject", cancellationToken);

            return new DeleteObjectResponse();
        }
        catch (Exception ex)
        {
            Logger.DeleteObjectFailed(ex, request.Bucket, request.Key);
            return Error.Failure("S3.DeleteObject.Failed", $"Failed to delete object {request.Key} from S3 bucket {request.Bucket}");
        }
    }

    /// <summary>
    /// Creates an HTTP request message targeting the specified object, marked for S3-style SigV4 signing.
    /// </summary>
    private HttpRequestMessage CreateObjectRequest(HttpMethod method, string bucket, string key)
    {
        var requestMessage = new HttpRequestMessage(method, BuildObjectUri(bucket, key));

        // S3 requires the canonical URI to be the once-encoded request path; other AWS services double-encode.
        requestMessage.Options.Set(HttpOptions.UseSingleUriEncoding, true);

        return requestMessage;
    }

    /// <summary>
    /// Builds the request URI for an object using virtual-host style addressing by default,
    /// or path-style addressing when a custom service URL is configured or ForcePathStyle is enabled.
    /// The bucket name and key are assumed to have already been validated via
    /// <see cref="ValidateBucketAndKey"/>.
    /// </summary>
    private Uri BuildObjectUri(string bucket, string key)
    {
        var encodedKey = EncodeKey(key);

        if (!string.IsNullOrWhiteSpace(Configuration.ServiceUrl) || Configuration.ForcePathStyle)
        {
            var baseUrl = _cachedPathStyleBaseUrl ??=
                Configuration.ServiceUrl?.TrimEnd('/') ?? $"https://s3.{Configuration.Region}.amazonaws.com";
            return new Uri($"{baseUrl}/{bucket}/{encodedKey}");
        }

        return new Uri($"https://{bucket}.s3.{Configuration.Region}.amazonaws.com/{encodedKey}");
    }

    /// <summary>
    /// URI-encodes an object key for use as the S3 canonical URI, preserving '/' separators.
    /// Each character is encoded exactly as the AWS SDKs encode S3 object keys: the RFC 3986
    /// unreserved set plus the sub-delimiters S3 leaves unescaped are passed through, everything
    /// else is percent-encoded from its UTF-8 bytes. Matching the AWS encoding byte-for-byte keeps
    /// the path Goa signs and sends identical to what AWS produces, so SigV4 validation succeeds.
    /// </summary>
    private static string EncodeKey(string key)
    {
        var requiresEncoding = false;
        foreach (var ch in key)
        {
            if (!IsS3UnreservedPathChar(ch))
            {
                requiresEncoding = true;
                break;
            }
        }

        if (!requiresEncoding)
            return key;

        var builder = new StringBuilder(key.Length + 16);
        Span<byte> utf8 = stackalloc byte[4];
        foreach (var rune in key.EnumerateRunes())
        {
            if (rune.IsAscii && IsS3UnreservedPathChar((char)rune.Value))
            {
                builder.Append((char)rune.Value);
                continue;
            }

            var written = rune.EncodeToUtf8(utf8);
            for (var i = 0; i < written; i++)
            {
                builder.Append('%');
                builder.Append(HexUpper[utf8[i] >> 4]);
                builder.Append(HexUpper[utf8[i] & 0xF]);
            }
        }

        return builder.ToString();
    }

    private const string HexUpper = "0123456789ABCDEF";

    /// <summary>
    /// Characters that are left unescaped inside an S3 canonical-URI path segment. This is the
    /// RFC 3986 unreserved set plus the sub-delimiters and '/' that the AWS SDKs preserve when
    /// encoding S3 object keys.
    /// </summary>
    private static bool IsS3UnreservedPathChar(char ch) =>
        ch is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9')
            or '-' or '.' or '_' or '~' or '/'
            or '!' or '$' or '&' or '\'' or '(' or ')' or '*' or '+' or ',' or ';' or '=';

    /// <summary>
    /// Validates the bucket name and object key before they are interpolated into the request URI.
    /// Rejects malformed bucket names and dot-segment keys that <see cref="Uri"/> would otherwise
    /// normalize away (which can escape the intended object and target the bucket itself).
    /// </summary>
    private static ErrorOr<Success> ValidateBucketAndKey(string? bucket, string? key)
    {
        var bucketResult = S3RequestValidation.ValidateBucketName(bucket);
        if (bucketResult.IsError)
            return bucketResult.Errors;

        return S3RequestValidation.ValidateKey(key);
    }

    /// <summary>
    /// Reads an error response body and maps the S3 XML error document to an <see cref="Error"/>.
    /// HEAD responses carry no body, so the status code alone is used in that case.
    /// </summary>
    private async Task<Error> ReadErrorAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        string? code = null;
        string? message = null;

        using var errorBuffer = await ReadResponseBytesAsync(response, cancellationToken);
        if (errorBuffer.Length > 0)
        {
            var errorPayload = Encoding.UTF8.GetString(errorBuffer.Span);
            var xmlError = new XmlApiError();
            xmlError.DeserializeFromXml(errorPayload);

            if (!string.IsNullOrWhiteSpace(xmlError.Code))
                code = xmlError.Code;
            if (!string.IsNullOrWhiteSpace(xmlError.Message))
                message = xmlError.Message;
        }

        Logger.RequestFailed($"Operation: {operation}, StatusCode: {(int)response.StatusCode}, Code: {code ?? "Unknown"}, Message: {message ?? "Unknown"}");

        if (response.StatusCode == HttpStatusCode.NotFound ||
            string.Equals(code, S3ErrorCodes.NoSuchKey, StringComparison.Ordinal) ||
            string.Equals(code, S3ErrorCodes.NoSuchBucket, StringComparison.Ordinal))
        {
            return Error.NotFound($"S3.{code ?? "NotFound"}", message ?? "The specified resource does not exist.");
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            return Error.Forbidden($"S3.{code ?? S3ErrorCodes.AccessDenied}", message ?? "Access denied.");
        }

        return Error.Failure(
            $"S3.{code ?? "Unknown"}",
            message ?? $"S3 {operation} request failed with status code {(int)response.StatusCode}.");
    }

    private static string? GetETag(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("ETag", out var values))
        {
            foreach (var value in values)
                return value;
        }

        return null;
    }

    private static Dictionary<string, string>? ReadMetadata(HttpResponseMessage response)
    {
        Dictionary<string, string>? metadata = null;

        foreach (var header in response.Headers.NonValidated)
        {
            if (header.Key.StartsWith(MetadataHeaderPrefix, StringComparison.OrdinalIgnoreCase))
            {
                metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata[header.Key[MetadataHeaderPrefix.Length..]] = header.Value.ToString();
            }
        }

        return metadata;
    }
}
