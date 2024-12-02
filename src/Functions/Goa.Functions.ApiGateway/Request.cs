using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Goa.Functions.ApiGateway;

/// <summary>
///     Summarises the request object for the invocation
/// </summary>
public sealed class Request
{
    /// <summary>
    /// Gets or sets the HTTP method used in the request (e.g., GET, POST).
    /// </summary>
    public required HttpMethod HttpMethod { get; init; }

    /// <summary>
    /// Gets or sets the headers included in the request as a dictionary.
    /// </summary>
    public required IReadOnlyDictionary<string, IEnumerable<string>>? Headers { get; init; }

    /// <summary>
    /// Gets or sets the query string parameters as a dictionary of single values.
    /// </summary>
    public required IReadOnlyDictionary<string, IEnumerable<string>>? QueryStringParameters { get; init; }

    /// <summary>
    /// Gets or sets the path parameters extracted from the request path.
    /// </summary>
    public required IReadOnlyDictionary<string, string>? PathParameters { get; init; }

    /// <summary>
    /// The values that got matched when matching the request object, if any
    /// </summary>
    public IReadOnlyDictionary<string,string>? RouteValues { get; set; }

    /// <summary>
    /// Gets or sets the body of the request.
    /// This is typically a JSON string, but can also be other content types like form data or plain text.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the body is Base64-encoded.
    /// This is required when the body contains binary data.
    /// </summary>
    public bool? IsBase64Encoded { get; init; }

    /// <summary>
    /// Gets or sets the full path of the request
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Gets or sets the stage variables defined for the API Gateway stage (common to both V1 and V2).
    /// </summary>
    public IReadOnlyDictionary<string, string>? StageVariables { get; init; }

    /// <summary>
    /// Gets or sets the user associated with the request if it can be found
    /// </summary>
    public ClaimsIdentity? User { get; set; }

    /// <summary>
    ///     Maps the request payload to a request object
    /// </summary>
    public static Request MapFrom(Payloads.V1.ProxyPayloadV1Request payload)
    {
        var headers = payload.MultiValueHeaders?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AsEnumerable()) ?? payload.Headers?.ToDictionary(kvp => kvp.Key, kvp => (IEnumerable<string>)new[] { kvp.Value }) ?? new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        return new Request
        {
            HttpMethod = new HttpMethod(payload.HttpMethod ?? "GET"), // Defaulting to GET if HttpMethod is null
            Headers = headers,
            QueryStringParameters = payload.MultiValueQueryStringParameters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AsEnumerable()) ?? payload.QueryStringParameters?.ToDictionary(kvp => kvp.Key, kvp => (IEnumerable<string>)new[] { kvp.Value }) ?? new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase),
            PathParameters = (payload.PathParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).AsReadOnly(),
            Body = payload.Body,
            IsBase64Encoded = payload.IsBase64Encoded,
            Path = payload.Path,
            StageVariables = (payload.StageVariables ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).AsReadOnly(),
            User = GetClaimsIdentityFromAuthHeader(headers)
        };
    }

    /// <summary>
    ///     Maps the request payload to a request object
    /// </summary>
    public static Request MapFrom(Payloads.V2.ProxyPayloadV2Request payload)
    {
        var headers = payload.Headers?.ToDictionary(kvp => kvp.Key, kvp => (IEnumerable<string>)new[] { kvp.Value }, StringComparer.OrdinalIgnoreCase) ?? new Dictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);
        return new Request
        {
            HttpMethod = new HttpMethod(payload.RequestContext?.Http?.Method ?? "GET"), // Defaulting to GET if HttpMethod is null
            Headers = headers,
            QueryStringParameters = payload.QueryStringParameters?.ToDictionary(kvp => kvp.Key, kvp => (IEnumerable<string>)new[] { kvp.Value }, StringComparer.OrdinalIgnoreCase),
            PathParameters = (payload.PathParameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).AsReadOnly(),
            Body = payload.Body,
            IsBase64Encoded = payload.IsBase64Encoded,
            Path = payload.RawPath,
            StageVariables = (payload.StageVariables ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)).AsReadOnly(),
            User = GetClaimsIdentityFromAuthHeader(headers)
        };
    }

    // Helper method to extract and parse the JWT token from the Authorization header
    private static ClaimsIdentity? GetClaimsIdentityFromAuthHeader(IDictionary<string, IEnumerable<string>> headers)
    {
        if (!headers.TryGetValue("Authorization", out var authorizationHeader))
        {
            return null;
        }

        var authHeaderValue = authorizationHeader.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeaderValue) || !authHeaderValue.StartsWith("Bearer "))
        {
            return null;
        }

        var token = authHeaderValue.Substring("Bearer ".Length).Trim();

        // Decode and validate the JWT token (assuming token validation is done here or elsewhere)
        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token))
        {
            return null;
        }

        var jwtToken = handler.ReadJwtToken(token);
        var claims = jwtToken.Claims.ToList();
        return new ClaimsIdentity(claims,  claims.Count > 0 ? "JWT" : "");
    }
}
