# Goa.Functions.ApiGateway.Authorizer

Build high-performance AWS Lambda authorizers for API Gateway with native AOT support. This package provides a fluent API for creating TOKEN and REQUEST type authorizers with flexible policy building, context passing, and mTLS certificate validation for secure API access control.

## Quick Start

```bash
dotnet new install Goa.Templates
dotnet new goa.authorizer -n "MyAuthorizer"
```

## Features

- **Native AOT Ready**: Optimized for ahead-of-time compilation with minimal cold starts
- **TOKEN and REQUEST Authorizers**: Support for both API Gateway authorizer types
- **Fluent PolicyBuilder**: Chainable API for building IAM policies with allow/deny rules
- **Context Passing**: Pass custom data to backend Lambda functions via the authorizer context
- **mTLS Support**: Access client certificate information for mutual TLS authentication
- **Usage Plan Integration**: Associate API keys with usage plans for throttling and quotas
- **Wildcard Patterns**: Allow or deny entire API stages with wildcard resource matching

## Basic Usage

### Token Authorizer

Process authorization tokens (e.g., JWT, API keys) extracted from a configured header:

```csharp
using Goa.Functions.ApiGateway.Authorizer;
using Goa.Functions.Core;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForAPIGatewayAuthorizer()
    .ForTokenAuthorizer()
    .HandleWith<ITokenValidator>(async (validator, authEvent) =>
    {
        // Validate the token from the configured header
        var claims = await validator.ValidateToken(authEvent.AuthorizationToken!);

        if (claims == null)
        {
            throw new UnauthorizedAccessException("Invalid token");
        }

        // Build the authorization policy
        return new PolicyBuilder(claims.Subject)
            .AllowAll(authEvent.MethodArn!)
            .WithContext("userId", claims.Subject)
            .WithContext("email", claims.Email)
            .Build();
    })
    .RunAsync();

public interface ITokenValidator
{
    Task<UserClaims?> ValidateToken(string token);
}

public record UserClaims(string Subject, string Email);
```

### Request Authorizer

Access full request details including headers, query parameters, and path parameters:

```csharp
using Goa.Functions.ApiGateway.Authorizer;
using Goa.Functions.Core;
using Microsoft.Extensions.Hosting;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForAPIGatewayAuthorizer()
    .ForRequestAuthorizer()
    .HandleWith<IRequestValidator>(async (validator, authEvent) =>
    {
        // Access headers, query parameters, or path parameters
        var apiKey = authEvent.Headers?["x-api-key"];
        var tenantId = authEvent.PathParameters?["tenantId"];

        var user = await validator.ValidateRequest(apiKey, tenantId);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid request");
        }

        // Build a policy with specific resource access
        var baseArn = PolicyBuilder.GetBaseArn(authEvent.MethodArn!);

        return new PolicyBuilder(user.Id)
            .Allow($"{baseArn}/GET/users/*")
            .Allow($"{baseArn}/POST/orders")
            .Deny($"{baseArn}/DELETE/*")
            .WithContext("tenantId", tenantId!)
            .WithContext("role", user.Role)
            .Build();
    })
    .RunAsync();

public interface IRequestValidator
{
    Task<User?> ValidateRequest(string? apiKey, string? tenantId);
}

public record User(string Id, string Role);
```

## PolicyBuilder API

The `PolicyBuilder` class provides a fluent interface for constructing IAM policy documents.

### Allow and Deny Methods

```csharp
// Allow a single resource
new PolicyBuilder("user-123")
    .Allow("arn:aws:execute-api:us-east-1:123456789:abc123/prod/GET/users")
    .Build();

// Allow multiple resources
new PolicyBuilder("user-123")
    .Allow(
        "arn:aws:execute-api:us-east-1:123456789:abc123/prod/GET/users",
        "arn:aws:execute-api:us-east-1:123456789:abc123/prod/POST/users"
    )
    .Build();

// Deny specific resources
new PolicyBuilder("user-123")
    .AllowAll(methodArn)
    .Deny("arn:aws:execute-api:us-east-1:123456789:abc123/prod/DELETE/users/*")
    .Build();
```

### Wildcard Patterns

Use `AllowAll` or `DenyAll` to match all resources under a method ARN:

```csharp
// Allow all endpoints in the API stage
new PolicyBuilder("user-123")
    .AllowAll(authEvent.MethodArn!)
    .Build();

// Deny all endpoints (explicit deny)
new PolicyBuilder("user-123")
    .DenyAll(authEvent.MethodArn!)
    .Build();
```

### Mixed Policies

Combine allow and deny statements for fine-grained access control:

```csharp
var baseArn = PolicyBuilder.GetBaseArn(authEvent.MethodArn!);

new PolicyBuilder("user-123")
    .Allow($"{baseArn}/GET/*")      // Allow all GET requests
    .Allow($"{baseArn}/POST/orders") // Allow POST to orders
    .Deny($"{baseArn}/DELETE/*")     // Deny all DELETE requests
    .Deny($"{baseArn}/*/admin/*")    // Deny all admin routes
    .Build();
```

### Context Passing

Pass custom data to backend Lambda functions. Values must be strings, numbers, or booleans:

```csharp
new PolicyBuilder("user-123")
    .AllowAll(methodArn)
    .WithContext("userId", "user-123")
    .WithContext("email", "user@example.com")
    .WithContext("isAdmin", true)
    .WithContext("rateLimit", 1000)
    .Build();
```

The context values are accessible in your backend Lambda via `$context.authorizer.<key>` in API Gateway mapping templates or via the request context in Lambda proxy integrations.

### Usage Plan Support

Associate an API key with API Gateway usage plans for throttling and quota enforcement:

```csharp
new PolicyBuilder("user-123")
    .AllowAll(methodArn)
    .WithUsageIdentifierKey("api-key-from-usage-plan")
    .Build();
```

### Building Resource ARNs

Use the static helper methods to construct or parse resource ARNs:

```csharp
// Build a complete resource ARN
var arn = PolicyBuilder.BuildResourceArn(
    region: "us-east-1",
    accountId: "123456789012",
    apiId: "abc123xyz",
    stage: "prod",
    verb: "GET",
    resource: "users/*"
);
// Result: arn:aws:execute-api:us-east-1:123456789012:abc123xyz/prod/GET/users/*

// Extract base ARN from a method ARN (removes method and resource portions)
var baseArn = PolicyBuilder.GetBaseArn(authEvent.MethodArn!);
// Input:  arn:aws:execute-api:us-east-1:123456789:abc123/prod/GET/users/123
// Result: arn:aws:execute-api:us-east-1:123456789:abc123/prod
```

## Event Models

### TokenAuthorizerEvent

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `string` | Always "TOKEN" |
| `AuthorizationToken` | `string?` | The token value from the configured header |
| `MethodArn` | `string?` | ARN of the incoming method request |

### RequestAuthorizerEvent

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `string` | Always "REQUEST" |
| `MethodArn` | `string?` | ARN of the incoming method request |
| `Resource` | `string?` | Resource path template |
| `Path` | `string?` | Request path |
| `HttpMethod` | `string?` | HTTP method (GET, POST, etc.) |
| `Headers` | `Dictionary<string, string>?` | Request headers |
| `QueryStringParameters` | `Dictionary<string, string>?` | Query string parameters |
| `PathParameters` | `Dictionary<string, string>?` | Path parameters |
| `StageVariables` | `Dictionary<string, string>?` | API Gateway stage variables |
| `RequestContext` | `AuthorizerRequestContext?` | Request context with identity info |

### AuthorizerRequestContext

| Property | Type | Description |
|----------|------|-------------|
| `Path` | `string?` | Resource path |
| `AccountId` | `string?` | AWS account ID |
| `ResourceId` | `string?` | Resource ID |
| `Stage` | `string?` | API Gateway stage name |
| `RequestId` | `string?` | Unique request ID |
| `Identity` | `AuthorizerIdentity?` | Identity information including client certificate |
| `ResourcePath` | `string?` | Resource path template |
| `HttpMethod` | `string?` | HTTP method |
| `ApiId` | `string?` | API Gateway API ID |

### mTLS Client Certificate

When using mutual TLS, access client certificate details via `RequestContext.Identity.ClientCert`:

| Property | Type | Description |
|----------|------|-------------|
| `ClientCertPem` | `string?` | PEM-encoded client certificate |
| `SubjectDN` | `string?` | Distinguished name of the subject |
| `IssuerDN` | `string?` | Distinguished name of the issuer |
| `SerialNumber` | `string?` | Certificate serial number |
| `Validity.NotBefore` | `string?` | Start of validity period |
| `Validity.NotAfter` | `string?` | End of validity period |

## Error Handling

Return authorization failures by throwing an exception. API Gateway interprets unhandled exceptions as authorization denials:

```csharp
.HandleWith<ITokenValidator>(async (validator, authEvent) =>
{
    if (string.IsNullOrEmpty(authEvent.AuthorizationToken))
    {
        // Throwing "Unauthorized" returns a 401 response
        throw new UnauthorizedAccessException("Unauthorized");
    }

    var isValid = await validator.ValidateToken(authEvent.AuthorizationToken);

    if (!isValid)
    {
        // Any exception denies access
        throw new UnauthorizedAccessException("Invalid token");
    }

    return new PolicyBuilder("user-id")
        .AllowAll(authEvent.MethodArn!)
        .Build();
})
```

For explicit deny policies (returning a 403 Forbidden), build a policy with deny statements:

```csharp
// Explicit deny returns 403 Forbidden
return new PolicyBuilder("user-id")
    .Deny(authEvent.MethodArn!)
    .Build();
```

## Documentation

For more information and examples, visit the [main Goa documentation](https://github.com/im5tu/goa).
