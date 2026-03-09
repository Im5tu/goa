using System.Net.Http.Headers;

namespace Goa.Clients.Core.Http;

/// <summary>
/// Lightweight representation of key AWS response headers.
/// </summary>
public sealed class ResponseHeaders
{
    /// <summary>
    /// Gets the AWS request ID from the response headers.
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>
    /// Gets the error type from the response headers.
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Gets the error message from the response headers.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the Content-Type of the response body.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Extracts key headers from an HTTP response.
    /// </summary>
    internal static ResponseHeaders FromHttpResponse(HttpResponseHeaders headers, HttpContentHeaders? contentHeaders = null)
    {
        string? requestId = null;
        if (headers.TryGetValues("x-amzn-RequestId", out var values)
            || headers.TryGetValues("x-amz-request-id", out values))
        {
            requestId = values.FirstOrDefault();
        }

        string? errorType = null;
        if (headers.TryGetValues("x-amzn-ErrorType", out var typeValues))
        {
            errorType = typeValues.FirstOrDefault();
        }

        string? errorMessage = null;
        if (headers.TryGetValues("x-amzn-ErrorMessage", out var errorValues))
        {
            errorMessage = errorValues.FirstOrDefault();
        }

        return new ResponseHeaders
        {
            RequestId = requestId,
            ErrorType = errorType,
            ErrorMessage = errorMessage,
            ContentType = contentHeaders?.ContentType?.MediaType
        };
    }
}
