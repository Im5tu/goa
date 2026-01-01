using Goa.Clients.Core.Credentials;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Goa.Clients.Core.Http;

/// <summary>
/// High-performance AWS v4 signature generation for HTTP requests with ultra-low memory allocation.
///
/// This implementation uses several advanced optimization techniques:
/// - Value-type SHA256 digest storage to avoid heap allocations for hash values
/// - ArrayPool-based buffer management to reduce GC pressure
/// - Insertion sort for small header collections to avoid IComparer allocation overhead
/// - Span-based string operations to eliminate intermediate allocations
/// - ASCII fast-path for UTF-8 encoding to optimize the common case
/// - Precise size calculations to minimize buffer over-allocation
///
/// Performance characteristics achieved:
/// - Sub-1KB total allocations for typical requests
/// - Zero allocation hash computation for payloads ≤1024 bytes
/// - Minimal string allocations through strategic span usage
/// </summary>
internal sealed class RequestSigner
{
    private static readonly string? _cachedAwsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
    private const string RootPath = "/";
    private const string CredentialSuffix = "/aws4_request";

    private readonly ICredentialProviderChain _credentialProvider;

    public RequestSigner(ICredentialProviderChain credentialProvider)
    {
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
    }

    /// <summary>
    /// Signs an HTTP request using AWS v4 signature algorithm.
    /// All async work is completed first, then synchronous span-heavy work is performed.
    /// This design avoids span-across-await-boundary issues while maintaining performance.
    /// </summary>
    public async ValueTask<string> SignRequestAsync(HttpRequestMessage request, DateTime? signingTime = null)
    {
        var time = signingTime ?? DateTime.UtcNow;

        var (region, serviceName) = GetRequestParameters(request);

        // Complete all async operations first to avoid span-across-await issues
        var credentialsResult = await _credentialProvider.GetCredentialsAsync().ConfigureAwait(false);
        if (credentialsResult.IsError)
        {
            var errs = credentialsResult.Errors;
            var sb = new StringBuilder(64);
            for (var i = 0; i < errs.Count; i++)
            {
                if (i != 0) sb.Append("; ");
                sb.Append(errs[i].Description);
            }
            ThrowInvalidOperationException($"Failed to retrieve AWS credentials: {sb}");
        }
        var credentials = credentialsResult.Value;

        // Async work done. Return value-type digest to avoid heap allocation across await boundary.
        var payloadHash = await ComputePayloadHashAsync(request).ConfigureAwait(false);

        // All span-heavy work happens here synchronously to maintain high performance.
        return ComputeSignatureCore(request, time, region, serviceName, credentials, payloadHash);
    }

    /// <summary>
    /// Signs an HTTP request using pre-fetched AWS credentials.
    /// This overload avoids async credential lookup and can complete fully synchronously
    /// when the payload is pre-computed via <see cref="HttpOptions.Payload"/>.
    /// </summary>
    public ValueTask<string> SignRequestAsync(HttpRequestMessage request, AwsCredentials credentials, DateTime? signingTime = null)
    {
        var time = signingTime ?? DateTime.UtcNow;
        var (region, serviceName) = GetRequestParameters(request);

        // Check if we can complete synchronously (null content or pre-computed payload)
        if (request.Content is null)
        {
            var payloadHash = ComputeEmptyPayloadHash();
            var signature = ComputeSignatureCore(request, time, region, serviceName, credentials, payloadHash);
            return ValueTask.FromResult(signature);
        }

        if (request.Options.TryGetValue(HttpOptions.Payload, out var payload) && payload?.Length > 0)
        {
            var payloadHash = ComputePayloadHashFromBytes(payload);
            var signature = ComputeSignatureCore(request, time, region, serviceName, credentials, payloadHash);
            return ValueTask.FromResult(signature);
        }

        // Fall back to async for streaming content
        return SignRequestWithCredentialsAsync(request, credentials, time, region, serviceName);
    }

    /// <summary>
    /// Computes hash of empty payload synchronously.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Sha256Hash ComputeEmptyPayloadHash()
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(ReadOnlySpan<byte>.Empty, hash);
        return Sha256Hash.FromBytes(hash);
    }

    /// <summary>
    /// Async fallback for signing when content must be streamed.
    /// </summary>
    private async ValueTask<string> SignRequestWithCredentialsAsync(
        HttpRequestMessage request,
        AwsCredentials credentials,
        DateTime time,
        string region,
        string serviceName)
    {
        var payloadHash = await ComputePayloadHashFromStreamAsync(request).ConfigureAwait(false);
        return ComputeSignatureCore(request, time, region, serviceName, credentials, payloadHash);
    }

    /// <summary>
    /// Computes the full authorization header value for a request.
    /// This method also adds required headers to the request for immediate usage.
    /// </summary>
    public async ValueTask<(string scheme, string token)> GetAuthorizationHeaderAsync(HttpRequestMessage request, DateTime? signingTime = null)
    {
        ArgumentNullException.ThrowIfNull(request);

        var time = signingTime ?? DateTime.UtcNow;
        var (region, serviceName) = GetRequestParameters(request);

        // Complete all async operations first
        var credentialsResult = await _credentialProvider.GetCredentialsAsync().ConfigureAwait(false);
        if (credentialsResult.IsError)
        {
            var errs = credentialsResult.Errors;
            var sb = new StringBuilder(64);
            for (var i = 0; i < errs.Count; i++)
            {
                if (i != 0) sb.Append("; ");
                sb.Append(errs[i].Description);
            }
            ThrowInvalidOperationException($"Failed to retrieve AWS credentials: {sb}");
        }
        var credentials = credentialsResult.Value;

        // Compute payload hash
        var payloadHash = await ComputePayloadHashAsync(request).ConfigureAwait(false);

        // Format dates
        Span<char> shortDate = stackalloc char[8];
        Span<char> longDate = stackalloc char[16];
        time.TryFormat(shortDate, out _, "yyyyMMdd".AsSpan());
        time.TryFormat(longDate, out _, "yyyyMMdd'T'HHmmss'Z'".AsSpan());

        // Add required headers to the request for immediate usage
        if (request.RequestUri != null)
            request.Headers.Host = request.RequestUri.Host;
        request.Headers.TryAddWithoutValidation(RequestHeaders.AmzDate, new string(longDate));

        Span<char> payloadHex = stackalloc char[64];
        BytesToHex(payloadHash.AsBytes, payloadHex);
        request.Headers.TryAddWithoutValidation(RequestHeaders.AmzContentSha256, new string(payloadHex));

        if (request.Options.TryGetValue(HttpOptions.Target, out var target) && !string.IsNullOrWhiteSpace(target))
        {
            request.Headers.TryAddWithoutValidation(RequestHeaders.AmzTarget, target);
        }

        if (request.Options.TryGetValue(HttpOptions.ApiVersion, out var apiVersion) && !string.IsNullOrWhiteSpace(apiVersion))
        {
            request.Headers.TryAddWithoutValidation(RequestHeaders.AmzApiVersion, apiVersion);
        }

        if (!string.IsNullOrWhiteSpace(credentials.SessionToken))
        {
            request.Headers.TryAddWithoutValidation(RequestHeaders.AmzSecurityToken, credentials.SessionToken);
        }

        // Now compute the signature
        var signature = ComputeSignatureCore(request, time, region, serviceName, credentials, payloadHash);

        // Build credential scope and signed headers list
        var scope = CreateCredentialScope(shortDate, region, serviceName);
        var signedHeaders = BuildSignedHeadersList(request, credentials);

        // Build final authorization header
        return ("AWS4-HMAC-SHA256", $"Credential={credentials.AccessKeyId}/{scope}, SignedHeaders={signedHeaders}, Signature={signature}");
    }

    /// <summary>
    /// Core synchronous signature computation using span-based operations for maximum performance.
    /// Uses precise size calculations and ArrayPool for efficient memory usage.
    /// </summary>
    private string ComputeSignatureCore(
        HttpRequestMessage request,
        DateTime time,
        string region,
        string serviceName,
        AwsCredentials credentials,
        Sha256Hash payloadHash)
    {
        // Format dates directly to stackalloc spans - zero allocation
        Span<char> shortDate = stackalloc char[8];
        Span<char> longDate  = stackalloc char[16];
        time.TryFormat(shortDate, out _, "yyyyMMdd".AsSpan());
        time.TryFormat(longDate,  out _, "yyyyMMdd'T'HHmmss'Z'".AsSpan());

        // ---- Build Canonical Request with precise memory management ----
        CountHeaders(request, out var reqCount, out var contentCount);
        var hasTarget = request.Options.TryGetValue(HttpOptions.Target, out var _target) && !string.IsNullOrWhiteSpace(_target);
        var hasApiVersion = request.Options.TryGetValue(HttpOptions.ApiVersion, out var _apiv) && !string.IsNullOrWhiteSpace(_apiv);

        var totalHeaders = ComputeHeaderTotal(request, credentials.SessionToken, hasTarget, hasApiVersion, reqCount, contentCount);

        // Rent arrays for header processing - returned at method end
        var headerRefs = ArrayPool<HeaderRef>.Shared.Rent(totalHeaders);
        var reqHeaders = ArrayPool<KeyValuePair<string, IEnumerable<string>>>.Shared.Rent(reqCount);
        var contentHeaders = ArrayPool<KeyValuePair<string, IEnumerable<string>>>.Shared.Rent(contentCount);

        int rhIndex = 0, chIndex = 0, hrefIndex = 0;

        // Build AWS required headers first
        if (request.RequestUri is not null)
            headerRefs[hrefIndex++] = HeaderRef.Create("host", HeaderKind.Host, -1);
        headerRefs[hrefIndex++] = HeaderRef.Create("x-amz-content-sha256", HeaderKind.AmzSha256, -1);
        headerRefs[hrefIndex++] = HeaderRef.Create("x-amz-date", HeaderKind.AmzDate, -1);

        if (hasApiVersion) headerRefs[hrefIndex++] = HeaderRef.Create("x-amz-api-version", HeaderKind.AmzApiVersion, -1);
        if (!string.IsNullOrWhiteSpace(credentials.SessionToken)) headerRefs[hrefIndex++] = HeaderRef.Create("x-amz-security-token", HeaderKind.AmzSecurityToken, -1);
        if (hasTarget) headerRefs[hrefIndex++] = HeaderRef.Create("x-amz-target", HeaderKind.AmzTarget, -1);

        // Add request headers, skipping AWS-managed headers to avoid duplicates
        foreach (var kv in request.Headers)
        {
            // Skip headers that are already handled by AWS-specific HeaderKind values
            if (IsAwsManagedHeader(kv.Key))
                continue;

            reqHeaders[rhIndex] = kv;
            headerRefs[hrefIndex++] = HeaderRef.Create(kv.Key, HeaderKind.Request, rhIndex);
            rhIndex++;
        }

        // Add content headers
        if (request.Content?.Headers is not null)
        {
            foreach (var kv in request.Content.Headers)
            {
                contentHeaders[chIndex] = kv;
                headerRefs[hrefIndex++] = HeaderRef.Create(kv.Key, HeaderKind.Content, chIndex);
                chIndex++;
            }
        }

        // Use insertion sort for small collections to avoid IComparer allocation overhead
        if (totalHeaders <= 16) InsertionSortHeaders(headerRefs.AsSpan(0, totalHeaders));
        else Array.Sort(headerRefs, 0, totalHeaders, HeaderRefComparer.OrdinalIgnoreCase);

        var pathStr = request.RequestUri?.AbsolutePath ?? RootPath;
        var canonUriLen = MeasureCanonicalUri(pathStr);

        // Process query parameters with efficient encoding
        var rawQuery = request.RequestUri?.Query;
        QueryPart[]? qparts = null;
        char[]? qEncodedBuf = null;
        var canonQueryLen = 0;

        if (!string.IsNullOrEmpty(rawQuery) && rawQuery!.Length > 1)
        {
            qparts = ArrayPool<QueryPart>.Shared.Rent(CountQueryParams(rawQuery));
            var qcount = ParseQuery(rawQuery, qparts);
            var totalEncodedChars = 0;
            for (var i = 0; i < qcount; i++)
            {
                ref var p = ref qparts[i];
                p.EncNameLen = MeasureRfc3986EncodeLen(rawQuery.AsSpan(p.Ns, p.Nl));
                p.EncValueLen = MeasureRfc3986EncodeLen(rawQuery.AsSpan(p.Vs, p.Vl));
                totalEncodedChars += p.EncNameLen + p.EncValueLen;
            }
            qEncodedBuf = ArrayPool<char>.Shared.Rent(totalEncodedChars);
            var off = 0;
            for (var i = 0; i < qcount; i++)
            {
                ref var p = ref qparts[i];
                p.EncNameOff = off;
                off += Rfc3986EncodeTo(rawQuery.AsSpan(p.Ns, p.Nl), qEncodedBuf.AsSpan(off));
                p.EncValueOff = off;
                off += Rfc3986EncodeTo(rawQuery.AsSpan(p.Vs, p.Vl), qEncodedBuf.AsSpan(off));
            }
            // In-place insertion sort with external buffer reference - no object allocation
            SortQueryParts(qparts.AsSpan(0, qcount), qEncodedBuf);
            canonQueryLen = 0;
            for (var i = 0; i < qcount; i++)
                canonQueryLen += qparts[i].EncNameLen + 1 + qparts[i].EncValueLen;
            if (qcount > 0) canonQueryLen += (qcount - 1);
        }

        var methodLen = request.Method.Method?.Length ?? 3;

        // Precise size calculation for canonical request to minimize buffer waste
        var canonicalEstimate =
            methodLen + 1 +
            canonUriLen + 1 +
            (canonQueryLen > 0 ? canonQueryLen : 0) + 1 +
            EstimateHeadersCharCount(headerRefs, totalHeaders, request, credentials, reqHeaders, contentHeaders) + 1 +
            EstimateSignedHeadersLen(headerRefs, totalHeaders) + 1 +
            64; // payload hash

        var rented = ArrayPool<char>.Shared.Rent(canonicalEstimate);
        var canonical = rented.AsSpan();
        var pos = 0;

        // Build canonical request using span operations - no string concatenation
        var m = request.Method.Method ?? "GET";
        m.AsSpan().CopyTo(canonical.Slice(pos)); pos += m.Length; canonical[pos++] = '\n';

        pos += WriteCanonicalUri(canonical.Slice(pos), pathStr);
        canonical[pos++] = '\n';

        if (qparts is null || qEncodedBuf is null)
        {
            canonical[pos++] = '\n';
        }
        else
        {
            pos += WriteCanonicalQuery(canonical.Slice(pos), qparts, qEncodedBuf);
            canonical[pos++] = '\n';
        }

        // Write canonical headers
#if DEBUG
        Console.WriteLine("=== GOA HEADERS IN CANONICAL REQUEST ===");
#endif
        for (var i = 0; i < totalHeaders; i++)
        {
#if DEBUG
            var headerName = headerRefs[i].Name.ToLowerInvariant();
#endif
            pos += WriteLowercase(canonical.Slice(pos), headerRefs[i].Name);
            canonical[pos++] = ':';
#if DEBUG
            var valueStartPos = pos;
#endif
            pos += WriteHeaderValue(canonical.Slice(pos), headerRefs[i], request, credentials, reqHeaders, contentHeaders, longDate, payloadHash.AsBytes);
#if DEBUG
            var headerValue = new string(canonical.Slice(valueStartPos, pos - valueStartPos));
            Console.WriteLine($"{headerName}:{headerValue}");
#endif
            canonical[pos++] = '\n';
        }
#if DEBUG
        Console.WriteLine("=== END GOA HEADERS ===");
#endif

        canonical[pos++] = '\n';

        // Write signed headers list
        for (var i = 0; i < totalHeaders; i++)
        {
            if (i != 0) canonical[pos++] = ';';
            pos += WriteLowercase(canonical.Slice(pos), headerRefs[i].Name);
        }
        canonical[pos++] = '\n';

        // Write payload hash hex directly from bytes - no intermediate string
        Span<char> payloadHex = stackalloc char[64];
        BytesToHex(payloadHash.AsBytes, payloadHex);
        payloadHex.CopyTo(canonical.Slice(pos)); pos += 64;

        // Debug: Output the complete canonical request
#if DEBUG
        var canonicalRequest = new string(canonical.Slice(0, pos));
        Console.WriteLine("=== GOA CANONICAL REQUEST ===");
        Console.WriteLine(canonicalRequest);
        Console.WriteLine("=== END CANONICAL REQUEST ===");
#endif

        // Hash canonical request directly to hex
        Span<char> canonHashHex = stackalloc char[64];
        ComputeSHA256HashDirectToSpan(canonical.Slice(0, pos), canonHashHex);
        ArrayPool<char>.Shared.Return(rented);

        // Clean up query processing arrays
        if (qparts is not null) ArrayPool<QueryPart>.Shared.Return(qparts, clearArray: false);
        if (qEncodedBuf is not null) ArrayPool<char>.Shared.Return(qEncodedBuf);

        // Build string-to-sign using stackalloc spans
        var scopeLen = shortDate.Length + 1 + region.Length + 1 + serviceName.Length + CredentialSuffix.Length;
        var stsLen = 16 + 1 + longDate.Length + 1 + scopeLen + 1 + 64;
        Span<char> sts = stackalloc char[stsLen];
        var sp = 0;
        "AWS4-HMAC-SHA256".AsSpan().CopyTo(sts.Slice(sp)); sp += 16; sts[sp++] = '\n';
        longDate.CopyTo(sts.Slice(sp)); sp += longDate.Length; sts[sp++] = '\n';
        shortDate.CopyTo(sts.Slice(sp)); sp += shortDate.Length; sts[sp++] = '/';
        region.AsSpan().CopyTo(sts.Slice(sp)); sp += region.Length; sts[sp++] = '/';
        serviceName.AsSpan().CopyTo(sts.Slice(sp)); sp += serviceName.Length;
        CredentialSuffix.AsSpan().CopyTo(sts.Slice(sp)); sp += CredentialSuffix.Length; sts[sp++] = '\n';
        canonHashHex.CopyTo(sts.Slice(sp)); sp += 64;

        // Debug: Output the string-to-sign
#if DEBUG
        var stringToSign = new string(sts.Slice(0, sp));
        Console.WriteLine("=== GOA STRING-TO-SIGN ===");
        Console.WriteLine(stringToSign);
        Console.WriteLine("=== END STRING-TO-SIGN ===");
        Console.WriteLine($"=== GOA CANONICAL REQUEST HASH: {new string(canonHashHex)} ===");
#endif

        // Generate final signature
        var signature = GetSignatureFromSpan(shortDate, sts.Slice(0, sp), credentials.SecretAccessKey, region, serviceName);

        // Debug: Output final signature
#if DEBUG
        Console.WriteLine($"=== GOA FINAL SIGNATURE: {signature} ===");
        Console.WriteLine($"=== GOA REGION: {region}, SERVICE: {serviceName} ===");
        Console.WriteLine($"=== GOA SHORT DATE: {new string(shortDate)}, LONG DATE: {new string(longDate)} ===");
#endif

        // Return rented arrays
        ArrayPool<HeaderRef>.Shared.Return(headerRefs);
        ArrayPool<KeyValuePair<string, IEnumerable<string>>>.Shared.Return(reqHeaders);
        ArrayPool<KeyValuePair<string, IEnumerable<string>>>.Shared.Return(contentHeaders);

        return signature;
    }

    /// <summary>
    /// Computes payload hash, returning synchronously when possible to avoid async state machine overhead.
    /// Uses streaming for large payloads to maintain low memory footprint.
    /// </summary>
    private static ValueTask<Sha256Hash> ComputePayloadHashAsync(HttpRequestMessage request)
    {
        if (request.Content is null)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(ReadOnlySpan<byte>.Empty, hash);
            return ValueTask.FromResult(Sha256Hash.FromBytes(hash));
        }

        // Check for pre-computed payload option first - can complete synchronously
        if (request.Options.TryGetValue(HttpOptions.Payload, out var payload) && payload?.Length > 0)
        {
            return ValueTask.FromResult(ComputePayloadHashFromBytes(payload));
        }

        // Stream large content asynchronously
        return ComputePayloadHashFromStreamAsync(request);
    }

    /// <summary>
    /// Computes payload hash synchronously from pre-computed UTF-8 bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Sha256Hash ComputePayloadHashFromBytes(byte[] payload)
    {
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(payload, hash);
        return Sha256Hash.FromBytes(hash);
    }

    /// <summary>
    /// Computes payload hash by streaming content asynchronously.
    /// Used when payload is not pre-computed and must be read from the request body.
    /// </summary>
    private static async ValueTask<Sha256Hash> ComputePayloadHashFromStreamAsync(HttpRequestMessage request)
    {
        using var stream = await request.Content!.ReadAsStreamAsync().ConfigureAwait(false);
        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        // Dynamic buffer sizing based on content length hint
        var contentLength = request.Content.Headers.ContentLength;
        var bufferSize = contentLength switch
        {
            null => 1024,           // Unknown size: 1KB (conservative)
            <= 8192 => 8192,        // Small: 8KB
            <= 65536 => 16384,      // Medium: 16KB
            <= 524288 => 65536,     // Large (up to 512KB): 64KB
            _ => 131072             // XLarge: 128KB
        };

        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false)) > 0)
                ih.AppendData(buffer, 0, read);

            Span<byte> hash = stackalloc byte[32];
            ih.TryGetHashAndReset(hash, out _);
            return Sha256Hash.FromBytes(hash);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    // ---- Query Parameter Processing ----

    /// <summary>
    /// Represents a parsed query parameter with offsets for efficient processing.
    /// </summary>
    private struct QueryPart
    {
        public int Ns, Nl;  // Name start/length in original query
        public int Vs, Vl;  // Value start/length in original query
        public int EncNameOff, EncNameLen;    // Encoded name offset/length in buffer
        public int EncValueOff, EncValueLen;  // Encoded value offset/length in buffer
    }

    /// <summary>
    /// In-place insertion sort for query parameters using external buffer.
    /// Avoids IComparer allocation while maintaining AWS canonicalization order.
    /// </summary>
    private static void SortQueryParts(Span<QueryPart> parts, char[] buf)
    {
        for (var i = 1; i < parts.Length; i++)
        {
            var key = parts[i];
            var j = i - 1;
            while (j >= 0 && Compare(parts[j], key, buf) > 0)
            {
                parts[j + 1] = parts[j];
                j--;
            }
            parts[j + 1] = key;
        }

        static int Compare(in QueryPart a, in QueryPart b, char[] buf)
        {
            ReadOnlySpan<char> an = buf.AsSpan(a.EncNameOff, a.EncNameLen);
            ReadOnlySpan<char> bn = buf.AsSpan(b.EncNameOff, b.EncNameLen);
            var c = an.CompareTo(bn, StringComparison.Ordinal);
            if (c != 0) return c;
            ReadOnlySpan<char> av = buf.AsSpan(a.EncValueOff, a.EncValueLen);
            ReadOnlySpan<char> bv = buf.AsSpan(b.EncValueOff, b.EncValueLen);
            return av.CompareTo(bv, StringComparison.Ordinal);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CountQueryParams(string query)
    {
        var n = 0;
        for (var i = 1; i < query.Length; i++) if (query[i] == '&') n++;
        return (query.Length > 1) ? n + 1 : 0;
    }

    private static int ParseQuery(string query, QueryPart[] parts)
    {
        var idx = 0;
        var start = 1; // skip '?'
        for (var i = 1; i <= query.Length; i++)
        {
            if (i == query.Length || query[i] == '&')
            {
                var eq = query.IndexOf('=', start, i - start);
                if (eq < 0) eq = i;
                parts[idx].Ns = start;
                parts[idx].Nl = eq - start;
                parts[idx].Vs = Math.Min(eq + 1, i);
                parts[idx].Vl = i - parts[idx].Vs;
                idx++;
                start = i + 1;
            }
        }
        return idx;
    }

    private static int WriteCanonicalQuery(Span<char> dest, QueryPart[] parts, char[] encBuf)
    {
        var pos = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            if (i != 0) dest[pos++] = '&';
            var p = parts[i];
            encBuf.AsSpan(p.EncNameOff, p.EncNameLen).CopyTo(dest.Slice(pos)); pos += p.EncNameLen;
            dest[pos++] = '=';
            encBuf.AsSpan(p.EncValueOff, p.EncValueLen).CopyTo(dest.Slice(pos)); pos += p.EncValueLen;
        }
        return pos;
    }

    // ---- URI Canonicalization ----

    private static int MeasureCanonicalUri(string rawPath)
    {
        if (string.IsNullOrEmpty(rawPath)) return 1;
        var len = 0;
        for (var i = 0; i < rawPath.Length; i++)
        {
            var ch = rawPath[i];
            if (ch == '/') { len++; continue; }
            len += IsUnreserved(ch) ? 1 : 3;
        }
        return len;
    }

    private static int WriteCanonicalUri(Span<char> dest, string rawPath)
    {
        if (string.IsNullOrEmpty(rawPath)) { dest[0] = '/'; return 1; }
        var pos = 0;
        for (var i = 0; i < rawPath.Length; i++)
        {
            var ch = rawPath[i];
            if (ch == '/') dest[pos++] = '/';
            else if (IsUnreserved(ch)) dest[pos++] = ch;
            else { PercentEncode(ch, dest.Slice(pos)); pos += 3; }
        }
        return pos;
    }

    // ---- Header Processing ----

    /// <summary>
    /// Writes header value with AWS-specific formatting and normalization.
    /// Handles different header types efficiently without string allocations.
    /// </summary>
    private static int WriteHeaderValue(
        Span<char> dest,
        in HeaderRef r,
        HttpRequestMessage req,
        AwsCredentials creds,
        KeyValuePair<string, IEnumerable<string>>[] reqHeaders,
        KeyValuePair<string, IEnumerable<string>>[] contentHeaders,
        ReadOnlySpan<char> longDate,
        ReadOnlySpan<byte> payloadHashBytes)
    {
        var pos = 0;
        switch (r.Kind)
        {
            case HeaderKind.Host:
            {
                var u = req.RequestUri!;
                pos += WriteHost(dest.Slice(pos), u);
                break;
            }
            case HeaderKind.AmzDate:
                longDate.CopyTo(dest); pos += longDate.Length; break;
            case HeaderKind.AmzSha256:
            {
                // Write hex directly from bytes - no string intermediate
                var tmp = dest.Slice(pos, 64);
                BytesToHex(payloadHashBytes, tmp);
                pos += 64;
                break;
            }
            case HeaderKind.AmzTarget:
                if (req.Options.TryGetValue(HttpOptions.Target, out var tgt) && !string.IsNullOrEmpty(tgt))
                { tgt.AsSpan().CopyTo(dest); pos += tgt.Length; }
                break;
            case HeaderKind.AmzApiVersion:
                if (req.Options.TryGetValue(HttpOptions.ApiVersion, out var apiv) && !string.IsNullOrEmpty(apiv))
                { apiv.AsSpan().CopyTo(dest); pos += apiv.Length; }
                break;
            case HeaderKind.AmzSecurityToken:
                if (!string.IsNullOrEmpty(creds.SessionToken))
                { creds.SessionToken.AsSpan().CopyTo(dest); pos += creds.SessionToken.Length; }
                break;
            case HeaderKind.Request:
                pos += WriteNormalizedJoined(dest, reqHeaders[r.Index].Value);
                break;
            case HeaderKind.Content:
                pos += WriteNormalizedJoined(dest, contentHeaders[r.Index].Value);
                break;
        }
        return pos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteHost(Span<char> dest, Uri u)
    {
        var pos = 0;
        var host = u.IdnHost ?? u.Host;
        host.AsSpan().CopyTo(dest); pos += host.Length;
        if (!u.IsDefaultPort && u.Port > 0)
        {
            dest[pos++] = ':';
            u.Port.TryFormat(dest.Slice(pos), out var w);
            pos += w;
        }
        return pos;
    }

    /// <summary>
    /// Normalizes and joins multiple header values according to AWS requirements.
    /// Collapses whitespace and joins with commas as per HTTP/AWS specifications.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteNormalizedJoined(Span<char> dest, IEnumerable<string> values)
    {
        var pos = 0; var firstVal = true;
        foreach (var raw in values)
        {
            if (!firstVal) dest[pos++] = ',';
            var s = raw.AsSpan().Trim();
            var pendingSpace = false;
            for (var i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                if (ch == ' ' || ch == '\t') { pendingSpace = true; }
                else
                {
                    if (pendingSpace && pos > 0 && dest[pos - 1] != ',') dest[pos++] = ' ';
                    dest[pos++] = ch;
                    pendingSpace = false;
                }
            }
            firstVal = false;
        }
        return pos;
    }

    private static int EstimateHeadersCharCount(
        HeaderRef[] refs, int count,
        HttpRequestMessage req, AwsCredentials creds,
        KeyValuePair<string, IEnumerable<string>>[] reqHeaders,
        KeyValuePair<string, IEnumerable<string>>[] contentHeaders)
    {
        var total = 0;
        for (var i = 0; i < count; i++)
        {
            var name = refs[i].Name;
            total += name.Length + 1 + 1; // name + ':' + '\n'
            switch (refs[i].Kind)
            {
                case HeaderKind.Host:
                    total += (req.RequestUri?.Host.Length ?? 0) + 6; break; // host + potential port
                case HeaderKind.AmzDate:
                    total += 16; break; // ISO timestamp length
                case HeaderKind.AmzSha256:
                    total += 64; break; // SHA256 hex length
                case HeaderKind.AmzTarget:
                    if (req.Options.TryGetValue(HttpOptions.Target, out var tgt) && tgt != null) total += tgt.Length; break;
                case HeaderKind.AmzApiVersion:
                    if (req.Options.TryGetValue(HttpOptions.ApiVersion, out var apiv) && apiv != null) total += apiv.Length; break;
                case HeaderKind.AmzSecurityToken:
                    total += creds.SessionToken?.Length ?? 0; break;
                case HeaderKind.Request:
                {
                    var kv = reqHeaders[refs[i].Index];
                    var len = 0; var first = true;
                    foreach (var v in kv.Value) { if (!first) len++; len += v?.Length ?? 0; first = false; }
                    total += len; break;
                }
                case HeaderKind.Content:
                {
                    var kv = contentHeaders[refs[i].Index];
                    var len = 0; var first = true;
                    foreach (var v in kv.Value) { if (!first) len++; len += v?.Length ?? 0; first = false; }
                    total += len; break;
                }
            }
        }
        return total;
    }

    private static int EstimateSignedHeadersLen(HeaderRef[] refs, int count)
    {
        var total = 0;
        for (var i = 0; i < count; i++)
        {
            total += refs[i].Name.Length;
            if (i != count - 1) total += 1; // semicolon separator
        }
        return total;
    }

    // ---- Encoding and Utility Functions ----

    /// <summary>
    /// RFC 3986 unreserved character check for URI encoding.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsUnreserved(char c)
        => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_' || c == '.' || c == '~';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void PercentEncode(char ch, Span<char> dest)
    {
        dest[0] = '%';
        var b = (byte)ch;
        dest[1] = (char)(b >> 4 < 10 ? '0' + (b >> 4) : 'A' + ((b >> 4) - 10));
        dest[2] = (char)((b & 0xF) < 10 ? '0' + (b & 0xF) : 'A' + ((b & 0xF) - 10));
    }

    private static int MeasureRfc3986EncodeLen(ReadOnlySpan<char> s)
    {
        var len = 0;
        for (var i = 0; i < s.Length; i++) len += IsUnreserved(s[i]) ? 1 : 3;
        return len;
    }

    private static int Rfc3986EncodeTo(ReadOnlySpan<char> s, Span<char> dest)
    {
        var pos = 0;
        for (var i = 0; i < s.Length; i++)
        {
            var ch = s[i];
            if (IsUnreserved(ch)) dest[pos++] = ch;
            else { PercentEncode(ch, dest.Slice(pos)); pos += 3; }
        }
        return pos;
    }

    /// <summary>
    /// Computes SHA256 hash and converts directly to hex span without intermediate string.
    /// Uses stackalloc for small inputs, ArrayPool for large inputs.
    /// </summary>
    private static void ComputeSHA256HashDirectToSpan(ReadOnlySpan<char> input, Span<char> hexOutput)
    {
        var byteCount = Encoding.UTF8.GetByteCount(input);
        Span<byte> hash = stackalloc byte[32];
        if (byteCount <= 1024) // Use stackalloc for small strings
        {
            Span<byte> bytes = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(input, bytes);
            SHA256.HashData(bytes, hash);
        }
        else // Use ArrayPool for larger strings
        {
            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var bytes = rented.AsSpan(0, byteCount);
                Encoding.UTF8.GetBytes(input, bytes);
                SHA256.HashData(bytes, hash);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }
        BytesToHex(hash, hexOutput);
    }

    /// <summary>
    /// High-performance bytes-to-hex conversion using bit manipulation.
    /// Produces lowercase hex output as required by AWS.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BytesToHex(ReadOnlySpan<byte> input, Span<char> hexOut)
    {
        for (var i = 0; i < input.Length; i++)
        {
            var b = input[i];
            // Bit manipulation formula for lowercase hex: avoids branching
            hexOut[i << 1] = (char)(87 + (b >> 4) + ((((b >> 4) - 10) >> 31) & -39));
            hexOut[(i << 1) + 1] = (char)(87 + (b & 0xF) + ((((b & 0xF) - 10) >> 31) & -39));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (string region, string serviceName) GetRequestParameters(HttpRequestMessage request)
    {
        // Use cached AWS_REGION environment variable when available
        var region = _cachedAwsRegion;
        if (request.Options.TryGetValue(HttpOptions.Region, out var regionOption) && !string.IsNullOrWhiteSpace(regionOption))
            region = regionOption;

        if (string.IsNullOrWhiteSpace(region))
            ThrowInvalidOperationException("Region is required");

        if (!request.Options.TryGetValue(HttpOptions.Service, out var service) || string.IsNullOrWhiteSpace(service))
            ThrowInvalidOperationException("Service name is required");

        return (region!, service);
    }

    /// <summary>
    /// Generates AWS v4 signature using HMAC-SHA256 chain with span-based operations.
    /// Avoids intermediate string allocations through careful span usage.
    /// </summary>
    private static string GetSignatureFromSpan(ReadOnlySpan<char> dateStamp, ReadOnlySpan<char> stringToSign,
        string secretAccessKey, string region, string service)
    {
        var prefix = "AWS4"u8;
        var request = "aws4_request"u8;
        var aws4KeyLength = prefix.Length + Encoding.UTF8.GetByteCount(secretAccessKey);

        // Build AWS4 + secret key on stack
        Span<byte> kSecret = stackalloc byte[aws4KeyLength];
        prefix.CopyTo(kSecret);
        Encoding.UTF8.GetBytes(secretAccessKey, kSecret.Slice(prefix.Length));

        // HMAC chain: kSecret -> kDate -> kRegion -> kService -> kSigning -> signature
        Span<byte> key = stackalloc byte[32];
        HmacSHA256FromSpan(key, kSecret, dateStamp);
        HmacSHA256Utf8(key, key, region);
        HmacSHA256Utf8(key, key, service);
        HmacSHA256(key, key, request);
        HmacSHA256FromSpan(key, key, stringToSign);

        // string.Create avoids intermediate char buffer allocation
        return string.Create(64, key.ToArray(), static (dst, bytes) =>
        {
            BytesToHex(bytes, dst);
        });
    }

    /// <summary>
    /// HMAC-SHA256 with span-based UTF-8 conversion.
    /// Uses stackalloc for small strings, ArrayPool for large strings.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HmacSHA256FromSpan(in Span<byte> destination, in ReadOnlySpan<byte> key, in ReadOnlySpan<char> data)
    {
        var byteCount = Encoding.UTF8.GetByteCount(data);
        if (byteCount <= 512) // Use stackalloc for small data
        {
            Span<byte> bytes = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(data, bytes);
            HMACSHA256.HashData(key, bytes, destination);
        }
        else // Use ArrayPool for large data
        {
            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var bytes = rented.AsSpan(0, byteCount);
                Encoding.UTF8.GetBytes(data, bytes);
                HMACSHA256.HashData(key, bytes, destination);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }
    }

    /// <summary>
    /// HMAC-SHA256 with ASCII fast-path optimization.
    /// Most AWS service names and regions are ASCII, so this optimization is frequently used.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HmacSHA256Utf8(in Span<byte> destination, in ReadOnlySpan<byte> key, string data)
    {
        // ASCII fast-path: avoid UTF-8 encoding for ASCII-only strings
        if (IsAscii(data))
        {
            Span<byte> bytes = stackalloc byte[data.Length];
            for (var i = 0; i < data.Length; i++) bytes[i] = (byte)data[i];
            HMACSHA256.HashData(key, bytes, destination);
            return;
        }

        // UTF-8 path for non-ASCII strings
        var byteCount = Encoding.UTF8.GetByteCount(data);
        if (byteCount <= 512)
        {
            Span<byte> bytes = stackalloc byte[byteCount];
            Encoding.UTF8.GetBytes(data, bytes);
            HMACSHA256.HashData(key, bytes, destination);
        }
        else
        {
            var rented = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                var bytes = rented.AsSpan(0, byteCount);
                Encoding.UTF8.GetBytes(data, bytes);
                HMACSHA256.HashData(key, bytes, destination);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAscii(string s)
    {
        for (var i = 0; i < s.Length; i++) if (s[i] > 0x7F) return false;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HmacSHA256(in Span<byte> destination, in ReadOnlySpan<byte> key, in ReadOnlySpan<byte> data)
        => HMACSHA256.HashData(key, data, destination);

    /// <summary>
    /// Insertion sort optimized for small header collections (≤16 items).
    /// Avoids IComparer allocation overhead that Array.Sort would incur.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InsertionSortHeaders(Span<HeaderRef> span)
    {
        for (var i = 1; i < span.Length; i++)
        {
            var key = span[i];
            var j = i - 1;
            while (j >= 0 && StringComparer.OrdinalIgnoreCase.Compare(span[j].Name, key.Name) > 0)
            {
                span[j + 1] = span[j];
                j--;
            }
            span[j + 1] = key;
        }
    }

    // ---- Value Types and Supporting Structures ----

    /// <summary>
    /// Value-type SHA256 hash storage using four 64-bit integers.
    /// Avoids heap allocation for hash values while maintaining efficient access to underlying bytes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private readonly struct Sha256Hash
    {
        private readonly ulong W0, W1, W2, W3;
        private Sha256Hash(ulong w0, ulong w1, ulong w2, ulong w3) { W0 = w0; W1 = w1; W2 = w2; W3 = w3; }

        /// <summary>
        /// Provides direct byte-level access to the hash without allocation.
        /// </summary>
        public ReadOnlySpan<byte> AsBytes
            => MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in W0), 4));

        public static Sha256Hash FromBytes(ReadOnlySpan<byte> b)
            => new(MemoryMarshal.Read<ulong>(b),
                   MemoryMarshal.Read<ulong>(b.Slice(8)),
                   MemoryMarshal.Read<ulong>(b.Slice(16)),
                   MemoryMarshal.Read<ulong>(b.Slice(24)));
    }

    /// <summary>
    /// Header reference with kind and index for efficient processing.
    /// Avoids allocating separate header processing objects.
    /// </summary>
    private readonly struct HeaderRef
    {
        public readonly string Name;
        public readonly HeaderKind Kind;
        public readonly int Index;  // Index into appropriate header array
        public static HeaderRef Create(string name, HeaderKind kind, int index) => new(name, kind, index);
        private HeaderRef(string name, HeaderKind kind, int index) { Name = name; Kind = kind; Index = index; }
    }

    /// <summary>
    /// Header kind enumeration for efficient header value writing.
    /// Avoids string comparisons and enables switch-based dispatch.
    /// </summary>
    private enum HeaderKind : byte
    {
        Host = 0,               // Special handling for host:port
        AmzDate = 1,           // ISO timestamp
        AmzSha256 = 2,         // Payload hash
        AmzTarget = 3,         // Service target
        AmzApiVersion = 4,     // API version
        AmzSecurityToken = 5,  // Session token
        Request = 6,           // Standard request header
        Content = 7            // Content header
    }

    /// <summary>
    /// IComparer implementation for header sorting fallback.
    /// Used only when header count exceeds insertion sort threshold (>16).
    /// </summary>
    private sealed class HeaderRefComparer : IComparer<HeaderRef>
    {
        public static readonly HeaderRefComparer OrdinalIgnoreCase = new();
        public int Compare(HeaderRef x, HeaderRef y) => StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
    }

    // ---- Utility Methods ----

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteLowercase(Span<char> dest, string s)
    {
        var i = 0;
        for (; i < s.Length; i++) dest[i] = char.ToLowerInvariant(s[i]);
        return i;
    }

    /// <summary>
    /// Counts request and content headers efficiently without allocation.
    /// </summary>
    private static void CountHeaders(HttpRequestMessage request, out int reqCount, out int contentCount)
    {
        reqCount = 0; contentCount = 0;
        foreach (var _ in request.Headers) reqCount++;
        if (request.Content?.Headers is not null)
            foreach (var _ in request.Content.Headers) contentCount++;
    }

    /// <summary>
    /// Computes total header count including AWS required headers.
    /// Used for precise ArrayPool sizing.
    /// </summary>
    private static int ComputeHeaderTotal(HttpRequestMessage req, string? sessionToken, bool hasTarget, bool hasApiVersion, int reqCount, int contentCount)
    {
        var total = 0;
        if (req.RequestUri != null) total++;     // host header
        total += 2;                              // x-amz-content-sha256, x-amz-date (always present)
        if (hasApiVersion) total++;              // x-amz-api-version
        if (!string.IsNullOrWhiteSpace(sessionToken)) total++; // x-amz-security-token
        if (hasTarget) total++;                  // x-amz-target

        // Count only non-AWS-managed headers from request headers
        var filteredReqCount = 0;
        foreach (var header in req.Headers)
        {
            if (!IsAwsManagedHeader(header.Key))
                filteredReqCount++;
        }

        total += filteredReqCount + contentCount;  // filtered user-provided headers
        return total;
    }

    /// <summary>
    /// Checks if a header is managed by AWS-specific HeaderKind values to avoid duplicates.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsAwsManagedHeader(string headerName)
    {
        return string.Equals(headerName, "Host", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(headerName, "x-amz-content-sha256", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(headerName, "x-amz-date", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(headerName, "x-amz-target", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(headerName, "x-amz-api-version", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(headerName, "x-amz-security-token", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Exception helpers with no-inlining to keep hot paths small.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowInvalidOperationException(string message) => throw new InvalidOperationException(message);

    [MethodImpl(MethodImplOptions.NoInlining)]
    [DoesNotReturn]
    private static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

    /// <summary>
    /// Creates credential scope string for AWS signature computation.
    /// </summary>
    private static string CreateCredentialScope(ReadOnlySpan<char> shortDate, string region, string serviceName)
    {
        ReadOnlySpan<char> charRegion = region;
        ReadOnlySpan<char> charService = serviceName;
        ReadOnlySpan<char> charSuffix = "/aws4_request";
        var length = shortDate.Length + 1 + charRegion.Length + 1 + charService.Length + charSuffix.Length;

        return string.Create(length, (shortDate: shortDate.ToString(), region, serviceName),
            static (chars, state) =>
            {
                ReadOnlySpan<char> charDate = state.shortDate;
                ReadOnlySpan<char> charRegion = state.region;
                ReadOnlySpan<char> charService = state.serviceName;
                ReadOnlySpan<char> charSuffix = "/aws4_request";

                var pos = 0;
                charDate.CopyTo(chars.Slice(pos)); pos += charDate.Length;
                chars[pos++] = '/';
                charRegion.CopyTo(chars.Slice(pos)); pos += charRegion.Length;
                chars[pos++] = '/';
                charService.CopyTo(chars.Slice(pos)); pos += charService.Length;
                charSuffix.CopyTo(chars.Slice(pos));
            });
    }

    /// <summary>
    /// Builds the signed headers list for the authorization header.
    /// </summary>
    private static string BuildSignedHeadersList(HttpRequestMessage request, AwsCredentials credentials)
    {
        var signedHeaders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

        // Add required AWS headers
        if (request.RequestUri != null) signedHeaders.Add("host");
        signedHeaders.Add("x-amz-date");
        signedHeaders.Add("x-amz-content-sha256");

        if (request.Options.TryGetValue(HttpOptions.Target, out var target) && !string.IsNullOrWhiteSpace(target))
        {
            signedHeaders.Add("x-amz-target");
        }

        if (request.Options.TryGetValue(HttpOptions.ApiVersion, out var apiVersion) && !string.IsNullOrWhiteSpace(apiVersion))
        {
            signedHeaders.Add("x-amz-api-version");
        }

        if (!string.IsNullOrWhiteSpace(credentials.SessionToken))
        {
            signedHeaders.Add("x-amz-security-token");
        }

        // Add request headers, skipping AWS-managed headers to match canonical request construction
        foreach (var header in request.Headers)
        {
            if (!IsAwsManagedHeader(header.Key))
                signedHeaders.Add(header.Key.ToLowerInvariant());
        }

        // Add content headers
        if (request.Content?.Headers != null)
        {
            foreach (var header in request.Content.Headers)
            {
                signedHeaders.Add(header.Key.ToLowerInvariant());
            }
        }

        return string.Join(";", signedHeaders);
    }
}
