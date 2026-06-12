namespace Goa.Clients.S3.Operations.PutObject;

/// <summary>
/// Builder for creating PutObject requests with a fluent API.
/// </summary>
public sealed class PutObjectBuilder
{
    private string? _bucket;
    private string? _key;
    private ReadOnlyMemory<byte> _body;
    private string? _contentType;
    private string? _serverSideEncryption;
    private string? _sseKmsKeyId;
    private Dictionary<string, string>? _metadata;

    /// <summary>
    /// Sets the bucket name.
    /// </summary>
    /// <param name="bucket">The bucket name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PutObjectBuilder WithBucket(string bucket)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        _bucket = bucket;
        return this;
    }

    /// <summary>
    /// Sets the object key.
    /// </summary>
    /// <param name="key">The object key.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PutObjectBuilder WithKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _key = key;
        return this;
    }

    /// <summary>
    /// Sets the object content.
    /// </summary>
    /// <param name="body">The object content.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PutObjectBuilder WithBody(ReadOnlyMemory<byte> body)
    {
        _body = body;
        return this;
    }

    /// <summary>
    /// Sets the MIME content type of the object.
    /// </summary>
    /// <param name="contentType">The content type (e.g. "application/json").</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PutObjectBuilder WithContentType(string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        _contentType = contentType;
        return this;
    }

    /// <summary>
    /// Sets the server-side encryption algorithm and optional KMS key ID.
    /// </summary>
    /// <param name="serverSideEncryption">The server-side encryption algorithm (e.g. "aws:kms").</param>
    /// <param name="sseKmsKeyId">The AWS KMS key ID to use when the algorithm is "aws:kms".</param>
    /// <returns>The builder instance for method chaining.</returns>
    public PutObjectBuilder WithServerSideEncryption(string serverSideEncryption, string? sseKmsKeyId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverSideEncryption);
        _serverSideEncryption = serverSideEncryption;
        _sseKmsKeyId = sseKmsKeyId;
        return this;
    }

    /// <summary>
    /// Adds a user-defined metadata entry, sent as an "x-amz-meta-{name}" header.
    /// </summary>
    /// <param name="name">The metadata name. Must be a valid HTTP token (no spaces, colons or control characters).</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The builder instance for method chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is not a valid HTTP token.</exception>
    public PutObjectBuilder AddMetadata(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        if (!IsValidHttpToken(name))
            throw new ArgumentException(
                $"Metadata name '{name}' is not a valid HTTP token. Names must not contain spaces, colons, control or separator characters.",
                nameof(name));

        _metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _metadata[name] = value;
        return this;
    }

    /// <summary>
    /// Determines whether a string is a valid HTTP token per RFC 7230 (used for header names).
    /// Invalid tokens would be silently dropped by the HTTP stack, so they are rejected up-front.
    /// </summary>
    private static bool IsValidHttpToken(string value)
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

    /// <summary>
    /// Builds the PutObject request.
    /// </summary>
    /// <returns>A configured PutObjectRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
    public PutObjectRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_bucket))
            throw new InvalidOperationException("Bucket is required.");

        if (string.IsNullOrWhiteSpace(_key))
            throw new InvalidOperationException("Key is required.");

        return new PutObjectRequest
        {
            Bucket = _bucket,
            Key = _key,
            Body = _body,
            ContentType = _contentType,
            ServerSideEncryption = _serverSideEncryption,
            SseKmsKeyId = _sseKmsKeyId,
            Metadata = _metadata
        };
    }
}
