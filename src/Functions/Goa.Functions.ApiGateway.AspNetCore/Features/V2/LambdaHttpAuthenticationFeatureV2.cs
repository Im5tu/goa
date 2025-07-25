using Goa.Functions.ApiGateway.Core.Payloads.V2;
using Microsoft.AspNetCore.Http.Features.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Goa.Functions.ApiGateway.AspNetCore.Features.V2;

internal sealed class LambdaHttpAuthenticationFeatureV2 : IHttpAuthenticationFeature
{
    public ClaimsPrincipal? User { get; set; }

    public LambdaHttpAuthenticationFeatureV2(ProxyPayloadV2Request request)
        : this(request.RequestContext?.Authentication, request.RequestContext?.Authorizer, request.Headers)
    {
    }

    public LambdaHttpAuthenticationFeatureV2(ProxyPayloadV2RequestAuthentication? authentication, ProxyPayloadV2RequestAuthorizer? authorizer, IDictionary<string, string>? headers)
    {
        var claims = new List<Claim>();

        if (authorizer?.Lambda is not null)
        {
            foreach (var (key, value) in authorizer.Lambda)
            {
                var strVal = value.ToString();
                if (!string.IsNullOrEmpty(strVal))
                    claims.Add(new Claim(key, strVal));
            }
        }

        if (authorizer?.Jwt?.Claims != null)
        {
            // Use the authorizer's JWT claims to build the ClaimsPrincipal
            User = new ClaimsPrincipal(new ClaimsIdentity(
                claims.Concat(authorizer.Jwt.Claims.Select(kvp => new Claim(kvp.Key, kvp.Value))),
                "Jwt"
            ));
        }
        else if (authorizer?.IAM is not null)
        {
            User = BuildIamPrincipal(authorizer.IAM, claims);
        }
        else if (headers != null && headers.TryGetValue("Authorization", out var authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            // Decode the JWT from the Authorization header
            var jwtToken = authHeader.Substring("Bearer ".Length).Trim();
            User = DecodeJwtToClaimsPrincipal(jwtToken, claims);
        }
        else if (authentication?.ClientCert != null)
        {
            // Fallback to client certificate details
            User = new ClaimsPrincipal(new ClaimsIdentity(claims.Concat(new[]
            {
                new Claim(ClaimTypes.Name, authentication.ClientCert.SubjectDN ?? "Anonymous"),
                new Claim("Issuer", authentication.ClientCert.IssuerDN ?? string.Empty),
                new Claim("SerialNumber", authentication.ClientCert.SerialNumber ?? string.Empty)
            }), "ClientCert"));
        }
        else
        {
            // Default to an anonymous principal
            User = new ClaimsPrincipal(new ClaimsIdentity());
        }
    }

    private ClaimsPrincipal DecodeJwtToClaimsPrincipal(string jwt, List<Claim> claims)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(jwt))
        {
            return new ClaimsPrincipal(new ClaimsIdentity()); // Invalid JWT, return empty principal
        }

        var token = handler.ReadJwtToken(jwt);

        // Create claims from JWT payload
        claims.AddRange(token.Claims.ToList());

        // Optionally add the token's subject as a Name claim if it exists
        if (!string.IsNullOrEmpty(token.Subject) && claims.All(c => c.Type != ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, token.Subject));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Jwt"));
    }

    private ClaimsPrincipal BuildIamPrincipal(ProxyPayloadV2IAMDescription iam, List<Claim> list)
    {
        var claims = new List<Claim>(list);

        if (!string.IsNullOrEmpty(iam.AccessKey))
        {
            claims.Add(new Claim("iam:AccessKey", iam.AccessKey));
        }

        if (!string.IsNullOrEmpty(iam.UserId))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, iam.UserId));
        }

        if (!string.IsNullOrEmpty(iam.UserARN))
        {
            claims.Add(new Claim("iam:UserARN", iam.UserARN));
        }

        if (!string.IsNullOrEmpty(iam.AccountId))
        {
            claims.Add(new Claim("iam:AccountId", iam.AccountId));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "IAM"));
    }
}
