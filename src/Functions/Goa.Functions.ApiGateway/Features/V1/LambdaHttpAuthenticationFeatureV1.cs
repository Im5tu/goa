using Goa.Functions.ApiGateway.Payloads;
using Goa.Functions.ApiGateway.Payloads.V1;
using Microsoft.AspNetCore.Http.Features.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Goa.Functions.ApiGateway.Features.V1;

internal sealed class LambdaHttpAuthenticationFeatureV1 : IHttpAuthenticationFeature
{
    public ClaimsPrincipal? User { get; set; }

    public LambdaHttpAuthenticationFeatureV1(ProxyPayloadV1Request request) : this(request.RequestContext?.Identity, request.RequestContext?.Authorizer, request.Headers)
    {
    }

    public LambdaHttpAuthenticationFeatureV1(ProxyPayloadV1RequestIdentity? identity, CustomAuthorizerContext? authorizer, IDictionary<string, string>? headers)
    {
        if (authorizer?.Claims != null)
        {
            // Use the authorizer context to build claims
            User = new ClaimsPrincipal(new ClaimsIdentity(
                authorizer.Claims.Select(kvp => new Claim(kvp.Key, kvp.Value)),
                "CustomAuthorizer"
            ));
        }
        else if (headers != null && headers.TryGetValue("Authorization", out var authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // Decode the JWT from the Authorization header
            var jwtToken = authHeader.Substring("Bearer ".Length).Trim();
            User = DecodeJwtToClaimsPrincipal(jwtToken);
        }
        // else if (identity != null)
        // {
        //     // Fallback to ProxyPayloadV1RequestIdentity
        //     User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        //     {
        //         new Claim(ClaimTypes.Name, identity.User ?? "Anonymous"),
        //         new Claim("SourceIp", identity.SourceIp ?? string.Empty)
        //     }, "IdentityContext"));
        // }
        else
        {
            // Default to an anonymous principal
            User = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    private ClaimsPrincipal DecodeJwtToClaimsPrincipal(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(jwt))
        {
            return new ClaimsPrincipal(new ClaimsIdentity()); // Invalid JWT, return empty principal
        }

        var token = handler.ReadJwtToken(jwt);

        // Create claims from JWT payload
        var claims = token.Claims.ToList();

        // Optionally add the token's subject as a Name claim if it exists
        if (!string.IsNullOrEmpty(token.Subject) && claims.All(c => c.Type != ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, token.Subject));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Jwt"));
    }
}
