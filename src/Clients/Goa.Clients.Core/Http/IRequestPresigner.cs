using ErrorOr;

namespace Goa.Clients.Core.Http;

/// <summary>
/// Generates AWS SigV4 pre-signed URLs (query-string signing) for AWS requests.
/// A pre-signed URL grants time-limited access to a single operation without the caller
/// needing AWS credentials, which lets bytes flow directly to/from the service instead of
/// being proxied through the signing application.
/// </summary>
/// <remarks>
/// The canonical URI is signed using single URI-encoding (S3 style): the path of
/// <see cref="PresignParameters.Uri"/> is signed as-is. This is correct for S3, the current
/// consumer. Services that require the canonical path to be double-encoded are not yet supported.
/// </remarks>
public interface IRequestPresigner
{
    /// <summary>
    /// Produces a pre-signed URL for the operation described by <paramref name="parameters"/>.
    /// The returned URL embeds the SigV4 authentication as query-string parameters
    /// (<c>X-Amz-Algorithm</c>, <c>X-Amz-Credential</c>, <c>X-Amz-Date</c>, <c>X-Amz-Expires</c>,
    /// <c>X-Amz-SignedHeaders</c>, an optional <c>X-Amz-Security-Token</c>, and <c>X-Amz-Signature</c>),
    /// using the <c>UNSIGNED-PAYLOAD</c> content hash.
    /// </summary>
    /// <param name="parameters">The request to pre-sign.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The fully-qualified pre-signed URL, or an error if credentials could not be resolved.</returns>
    ValueTask<ErrorOr<string>> PresignAsync(PresignParameters parameters, CancellationToken cancellationToken = default);
}

/// <summary>
/// Describes a single AWS operation to be pre-signed.
/// </summary>
public sealed class PresignParameters
{
    /// <summary>
    /// The HTTP method the caller will use against the pre-signed URL (e.g. GET, PUT).
    /// </summary>
    public required HttpMethod Method { get; init; }

    /// <summary>
    /// The scheme, host and (already-encoded) path of the target resource. Any query string on this
    /// URI is ignored; supply signed query parameters via <see cref="QueryParameters"/> instead.
    /// </summary>
    public required Uri Uri { get; init; }

    /// <summary>
    /// The AWS region used for the SigV4 credential scope (e.g. "us-east-1").
    /// </summary>
    public required string Region { get; init; }

    /// <summary>
    /// The AWS service name used for SigV4 signing (e.g. "s3").
    /// </summary>
    public required string Service { get; init; }

    /// <summary>
    /// How long the generated URL remains valid. Clamped to the SigV4 maximum of 7 days.
    /// </summary>
    public required TimeSpan Expiry { get; init; }

    /// <summary>
    /// Additional headers, beyond the mandatory <c>host</c>, that the caller commits to sending when
    /// using the URL (e.g. <c>content-length</c> to bind an exact upload size). Header names are
    /// treated case-insensitively; the caller MUST send matching values or the signature will fail.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>>? SignedHeaders { get; init; }

    /// <summary>
    /// Additional query-string parameters to include in, and sign as part of, the URL
    /// (e.g. <c>response-content-disposition</c> for a download). Values are supplied unencoded.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, string>>? QueryParameters { get; init; }
}
