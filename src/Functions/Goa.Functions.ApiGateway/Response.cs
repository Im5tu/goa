namespace Goa.Functions.ApiGateway;

/// <summary>
///     Represents the response that will be returned to the Lambda Runtime API
/// </summary>
public class Response
{
    private readonly Dictionary<string, List<string>> _headers = new();
    private readonly List<string> _cookies = new();

    /// <summary>
    ///     Represents the HTTP Result, and object, of the given request
    /// </summary>
    public HttpResult? Result { get; set; }
    /// <summary>
    ///     The headers to return to the requester
    /// </summary>
    public IEnumerable<KeyValuePair<string, List<string>>> Headers => _headers;
    /// <summary>
    ///     The HTTP Cookies to set
    /// </summary>
    public IEnumerable<string> Cookies => _cookies;
    /// <summary>
    ///     The exception that occured during the request context
    /// </summary>
    public Exception? Exception { get; set; }
    /// <summary>
    ///     Whether the request was handled by middleware or not
    /// </summary>
    public bool ExceptionHandled { get; set; }

    /// <summary>
    ///     Adds the specified header to the response
    /// </summary>
    public void AddHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            return;

        if (!_headers.TryGetValue(key, out var headers))
            _headers[key] = headers = new List<string>();

        headers.Add(value);
    }

    /// <summary>
    ///     Attempts to add the specified header. Returns false if the key already exists or the key/value are null or whitespace.
    /// </summary>
    public bool TryAddHeader(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            return false;

        return _headers.TryAdd(key, new() { value });
    }

    /// <summary>
    ///     Adds a HTTP Cookie to the response
    /// </summary>
    public void AddCookie(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        _cookies.Add(value);
    }
}
