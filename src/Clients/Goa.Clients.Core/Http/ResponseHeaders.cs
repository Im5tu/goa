using System.Net.Http.Headers;

namespace Goa.Clients.Core.Http;

/// <summary>
/// Lightweight representation of key AWS response headers.
/// </summary>
public sealed class ResponseHeaders
{
    private const string AmznRequestId = "x-amzn-RequestId";
    private const string AmzRequestId = "x-amz-request-id";
    private const string AmznErrorType = "x-amzn-ErrorType";
    private const string AmznErrorMessage = "x-amzn-ErrorMessage";

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
        if (headers.TryGetValues(AmznRequestId, out var values)
            || headers.TryGetValues(AmzRequestId, out values))
        {
            foreach (var v in values) { requestId = v; break; }
        }

        string? errorType = null;
        if (headers.TryGetValues(AmznErrorType, out var typeValues))
        {
            foreach (var v in typeValues) { errorType = v; break; }
        }

        string? errorMessage = null;
        if (headers.TryGetValues(AmznErrorMessage, out var errorValues))
        {
            foreach (var v in errorValues) { errorMessage = v; break; }
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
