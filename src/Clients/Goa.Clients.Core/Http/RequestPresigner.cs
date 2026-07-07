using ErrorOr;
using Goa.Clients.Core.Credentials;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Goa.Clients.Core.Http;

/// <summary>
/// Default <see cref="IRequestPresigner"/> implementation producing AWS SigV4 query-string
/// pre-signed URLs. Uses the <c>UNSIGNED-PAYLOAD</c> content hash, which is the standard for
/// pre-signed URLs where the body is transferred directly between the caller and the service.
/// </summary>
internal sealed class RequestPresigner : IRequestPresigner
{
    private const string Algorithm = "AWS4-HMAC-SHA256";
    private const string UnsignedPayload = "UNSIGNED-PAYLOAD";
    private const string CredentialSuffix = "aws4_request";

    // SigV4 caps X-Amz-Expires at 7 days.
    private static readonly TimeSpan MaxExpiry = TimeSpan.FromDays(7);

    private readonly ICredentialProviderChain _credentialProvider;
    private readonly TimeProvider _timeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestPresigner"/> class.
    /// </summary>
    /// <param name="credentialProvider">The credential chain used to sign URLs.</param>
    /// <param name="timeProvider">
    /// Time source for the signing timestamp. Defaults to <see cref="TimeProvider.System"/>;
    /// injectable so signatures can be verified deterministically in tests.
    /// </param>
    public RequestPresigner(ICredentialProviderChain credentialProvider, TimeProvider? timeProvider = null)
    {
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async ValueTask<ErrorOr<string>> PresignAsync(PresignParameters parameters, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.Uri);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.Region);
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.Service);

        // Reserved-name guards: the X-Amz-* query params and the host header are produced and signed
        // by this method. Letting a caller also supply them would emit duplicates and break the URL.
        if (parameters.QueryParameters is { Count: > 0 })
        {
            foreach (var param in parameters.QueryParameters)
            {
                if (param.Key.StartsWith("X-Amz-", StringComparison.OrdinalIgnoreCase))
                    return Error.Validation("Presign.ReservedQueryParameter", $"Query parameter '{param.Key}' is reserved for SigV4 signing and cannot be supplied by the caller.");
            }
        }
        if (parameters.SignedHeaders is { Count: > 0 })
        {
            foreach (var header in parameters.SignedHeaders)
            {
                if (string.Equals(header.Key, "host", StringComparison.OrdinalIgnoreCase))
                    return Error.Validation("Presign.ReservedHeader", "The 'host' header is always signed and cannot be supplied by the caller.");
            }
        }

        var credentialsResult = await _credentialProvider.GetCredentialsAsync().ConfigureAwait(false);
        if (credentialsResult.IsError)
            return credentialsResult.Errors;

        var credentials = credentialsResult.Value;

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var amzDate = now.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        var dateStamp = now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        var expirySeconds = (long)Math.Clamp(parameters.Expiry.TotalSeconds, 1, MaxExpiry.TotalSeconds);

        var uri = parameters.Uri;
        var canonicalUri = uri.AbsolutePath.Length == 0 ? "/" : uri.AbsolutePath;

        // ---- Canonical headers (always host, plus any caller-committed headers) ----
        var headers = new List<KeyValuePair<string, string>>(1 + (parameters.SignedHeaders?.Count ?? 0))
        {
            new("host", FormatHost(uri))
        };
        if (parameters.SignedHeaders is { Count: > 0 })
        {
            foreach (var header in parameters.SignedHeaders)
                headers.Add(new KeyValuePair<string, string>(header.Key.ToLowerInvariant(), NormalizeHeaderValue(header.Value)));
        }
        headers.Sort(static (a, b) => string.CompareOrdinal(a.Key, b.Key));

        var signedHeaderNames = string.Join(';', headers.Select(static h => h.Key));

        var canonicalHeaders = new StringBuilder();
        foreach (var header in headers)
            canonicalHeaders.Append(header.Key).Append(':').Append(header.Value).Append('\n');

        // ---- Canonical query (all X-Amz-* auth params plus caller query params, sorted by encoded name) ----
        var scope = $"{dateStamp}/{parameters.Region}/{parameters.Service}/{CredentialSuffix}";

        var query = new List<KeyValuePair<string, string>>(6 + (parameters.QueryParameters?.Count ?? 0))
        {
            new("X-Amz-Algorithm", Algorithm),
            new("X-Amz-Credential", $"{credentials.AccessKeyId}/{scope}"),
            new("X-Amz-Date", amzDate),
            new("X-Amz-Expires", expirySeconds.ToString(CultureInfo.InvariantCulture)),
            new("X-Amz-SignedHeaders", signedHeaderNames)
        };
        if (!string.IsNullOrWhiteSpace(credentials.SessionToken))
            query.Add(new KeyValuePair<string, string>("X-Amz-Security-Token", credentials.SessionToken!));
        if (parameters.QueryParameters is { Count: > 0 })
        {
            foreach (var param in parameters.QueryParameters)
                query.Add(new KeyValuePair<string, string>(param.Key, param.Value ?? string.Empty));
        }

        var canonicalQuery = BuildCanonicalQuery(query);

        // ---- Canonical request -> string to sign -> signature ----
        var canonicalRequest = new StringBuilder()
            .Append(parameters.Method.Method).Append('\n')
            .Append(canonicalUri).Append('\n')
            .Append(canonicalQuery).Append('\n')
            .Append(canonicalHeaders).Append('\n')
            .Append(signedHeaderNames).Append('\n')
            .Append(UnsignedPayload)
            .ToString();

        var canonicalRequestHash = ToHexLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRequest)));

        var stringToSign = $"{Algorithm}\n{amzDate}\n{scope}\n{canonicalRequestHash}";

        var signingKey = DeriveSigningKey(credentials.SecretAccessKey, dateStamp, parameters.Region, parameters.Service);
        var signature = ToHexLower(HmacSha256(signingKey, Encoding.UTF8.GetBytes(stringToSign)));

        // Reuse the already-encoded canonical query for the URL, then append the signature.
        var url = $"{uri.GetLeftPart(UriPartial.Path)}?{canonicalQuery}&X-Amz-Signature={signature}";
        return url;
    }

    /// <summary>
    /// Canonicalizes a header value per SigV4: trim outer whitespace and collapse each run of
    /// internal whitespace to a single space, matching the normalization S3 applies before it
    /// recomputes the signature.
    /// </summary>
    private static string NormalizeHeaderValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var trimmed = value.AsSpan().Trim();
        var builder = new StringBuilder(trimmed.Length);
        var pendingSpace = false;
        foreach (var ch in trimmed)
        {
            if (ch is ' ' or '\t')
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace && builder.Length > 0)
                builder.Append(' ');
            pendingSpace = false;
            builder.Append(ch);
        }
        return builder.ToString();
    }

    private static string FormatHost(Uri uri)
    {
        var host = uri.IdnHost;
        return uri.IsDefaultPort || uri.Port <= 0 ? host : $"{host}:{uri.Port}";
    }

    private static string BuildCanonicalQuery(List<KeyValuePair<string, string>> query)
    {
        var encoded = new List<KeyValuePair<string, string>>(query.Count);
        foreach (var param in query)
            encoded.Add(new KeyValuePair<string, string>(Rfc3986Encode(param.Key), Rfc3986Encode(param.Value)));

        encoded.Sort(static (a, b) =>
        {
            var byName = string.CompareOrdinal(a.Key, b.Key);
            return byName != 0 ? byName : string.CompareOrdinal(a.Value, b.Value);
        });

        var builder = new StringBuilder();
        for (var i = 0; i < encoded.Count; i++)
        {
            if (i != 0) builder.Append('&');
            builder.Append(encoded[i].Key).Append('=').Append(encoded[i].Value);
        }
        return builder.ToString();
    }

    private static byte[] DeriveSigningKey(string secretAccessKey, string dateStamp, string region, string service)
    {
        var kDate = HmacSha256(Encoding.UTF8.GetBytes("AWS4" + secretAccessKey), Encoding.UTF8.GetBytes(dateStamp));
        var kRegion = HmacSha256(kDate, Encoding.UTF8.GetBytes(region));
        var kService = HmacSha256(kRegion, Encoding.UTF8.GetBytes(service));
        return HmacSha256(kService, Encoding.UTF8.GetBytes(CredentialSuffix));
    }

    private static byte[] HmacSha256(byte[] key, byte[] data) => HMACSHA256.HashData(key, data);

    private static string ToHexLower(byte[] bytes) => Convert.ToHexStringLower(bytes);

    /// <summary>
    /// RFC 3986 percent-encoding over the UTF-8 bytes of <paramref name="value"/>, leaving only the
    /// unreserved set (<c>A-Z a-z 0-9 - _ . ~</c>) unescaped. This is the encoding AWS SigV4 requires
    /// for canonical query-string names and values (note: space encodes to <c>%20</c>, not <c>+</c>).
    /// </summary>
    private static string Rfc3986Encode(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var builder = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
        {
            if (IsUnreserved(b))
                builder.Append((char)b);
            else
                builder.Append('%').Append(HexUpper[b >> 4]).Append(HexUpper[b & 0xF]);
        }
        return builder.ToString();
    }

    private const string HexUpper = "0123456789ABCDEF";

    private static bool IsUnreserved(byte b) =>
        b is (>= (byte)'A' and <= (byte)'Z') or (>= (byte)'a' and <= (byte)'z') or (>= (byte)'0' and <= (byte)'9')
            or (byte)'-' or (byte)'_' or (byte)'.' or (byte)'~';
}
