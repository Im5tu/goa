using System.Net;

namespace Goa.Functions.ApiGateway;

/// <summary>
///     Create a HTTP Result with the specified status
/// </summary>
public record HttpResult(HttpStatusCode StatusCode)
{
    /// <summary>
    ///     The object that will be returned to the consumer
    /// </summary>
    public object? ResponseBody { get; init; }

    #region "2xx"

    /// <summary>
    /// Equivalent to HTTP status 200. OK indicates that the request succeeded and that the requested information is in the response. This is the most common status code to receive.
    /// </summary>
    public static HttpResult Ok(object? responseBody = null) => new(HttpStatusCode.OK) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 201. Created indicates that the request resulted in a new resource created before the response was sent.
    /// </summary>
    public static HttpResult Created(object? responseBody = null) => new(HttpStatusCode.Created) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 202. Accepted indicates that the request has been accepted for further processing.
    /// </summary>
    public static HttpResult Accepted(object? responseBody = null) => new(HttpStatusCode.Accepted) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 204. NoContent indicates that the request has been successfully processed and that the response is intentionally blank.
    /// </summary>
    public static HttpResult NoContent(object? responseBody = null) => new(HttpStatusCode.NoContent) { ResponseBody = responseBody };

    #endregion

    #region "3xx"

    /// <summary>
    /// Equivalent to HTTP status 301. MovedPermanently indicates that the requested information has been moved to the URI specified in the Location header. The default action when this status is received is to follow the Location header associated with the response. MovedPermanently is a synonym for Moved.
    /// </summary>
    public static HttpResult MovedPermanently(object? responseBody = null) => new(HttpStatusCode.MovedPermanently) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 303. RedirectMethod automatically redirects the client to the URI specified in the Location header as the result of a POST. The request to the resource specified by the Location header will be made with a GET. RedirectMethod is a synonym for SeeOther.
    /// </summary>
    public static HttpResult Redirect(object? responseBody = null) => new(HttpStatusCode.Redirect) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 304. NotModified indicates that the client's cached copy is up to date. The contents of the resource are not transferred.
    /// </summary>
    public static HttpResult NotModified(object? responseBody = null) => new(HttpStatusCode.NotModified) { ResponseBody = responseBody };

    #endregion

    #region "4xx"

    /// <summary>
    /// Equivalent to HTTP status 400. BadRequest indicates that the request could not be understood by the server. BadRequest is sent when no other error is applicable, or if the exact error is unknown or does not have its own error code.
    /// </summary>
    public static HttpResult BadRequest(object? responseBody = null) => new(HttpStatusCode.BadRequest) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 401. Unauthorized indicates that the requested resource requires authentication. The WWW-Authenticate header contains the details of how to perform the authentication.
    /// </summary>
    public static HttpResult Unauthorized(object? responseBody = null) => new(HttpStatusCode.Unauthorized) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 403. Forbidden indicates that the server refuses to fulfill the request.
    /// </summary>
    public static HttpResult Forbidden(object? responseBody = null) => new(HttpStatusCode.Forbidden) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 404. NotFound indicates that the requested resource does not exist on the server.
    /// </summary>
    public static HttpResult NotFound(object? responseBody = null) => new(HttpStatusCode.NotFound) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 405. MethodNotAllowed indicates that the request method (POST or GET) is not allowed on the requested resource.
    /// </summary>
    public static HttpResult MethodNotAllowed(object? responseBody = null) => new(HttpStatusCode.MethodNotAllowed) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 406. NotAcceptable indicates that the client has indicated with Accept headers that it will not accept any of the available representations of the resource.
    /// </summary>
    public static HttpResult NotAcceptable(object? responseBody = null) => new(HttpStatusCode.NotAcceptable) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 415. UnsupportedMediaType indicates that the request is an unsupported type.
    /// </summary>
    public static HttpResult UnsupportedMediaType(object? responseBody = null) => new(HttpStatusCode.UnsupportedMediaType) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 422. UnprocessableEntity indicates that the request was well-formed but was unable to be followed due to semantic errors. UnprocessableEntity is a synonym for UnprocessableContent.
    /// </summary>
    public static HttpResult UnprocessableEntity(object? responseBody = null) => new(HttpStatusCode.UnprocessableEntity) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 429. TooManyRequests indicates that the user has sent too many requests in a given amount of time.
    /// </summary>
    public static HttpResult TooManyRequests(object? responseBody = null) => new(HttpStatusCode.TooManyRequests) { ResponseBody = responseBody };

    #endregion

    #region "5xx"

    /// <summary>
    /// Equivalent to HTTP status 500. InternalServerError indicates that a generic error has occurred on the server.
    /// </summary>
    public static HttpResult InternalServerError(object? responseBody = null) => new(HttpStatusCode.InternalServerError) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 501. NotImplemented indicates that the server does not support the requested function.
    /// </summary>
    public static HttpResult NotImplemented(object? responseBody = null) => new(HttpStatusCode.NotImplemented) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 502. BadGateway indicates that an intermediate proxy server received a bad response from another proxy or the origin server.
    /// </summary>
    public static HttpResult BadGateway(object? responseBody = null) => new(HttpStatusCode.BadGateway) { ResponseBody = responseBody };

    /// <summary>
    /// Equivalent to HTTP status 503. ServiceUnavailable indicates that the server is temporarily unavailable, usually due to high load or maintenance.
    /// </summary>
    public static HttpResult ServiceUnavailable(object? responseBody = null) => new(HttpStatusCode.ServiceUnavailable) { ResponseBody = responseBody };

    #endregion
}
