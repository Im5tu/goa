using Goa.Functions.ApiGateway.Authorizer;
using Goa.Functions.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

await Host.CreateDefaultBuilder()
    .UseLambdaLifecycle()
    .ForAPIGatewayAuthorizer()
#if (authorizerType == "token")
    .ForTokenAuthorizer()
    .HandleWith<ILoggerFactory>((handler, evt) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that handles authorization logic
        var logger = handler.CreateLogger("TokenAuthorizer");

        // Access the authorization token from the event
        var token = evt.AuthorizationToken;
        var methodArn = evt.MethodArn;

        logger.LogInformation("Authorizing token for method: {MethodArn}", methodArn);

        // TODO :: Implement your token validation logic here
        // Example: Validate JWT token, check against database, etc.
        var isValid = ValidateToken(token);

        if (!isValid)
        {
            // Return a deny policy
            return Task.FromResult(
                new PolicyBuilder("user")
                    .Deny(methodArn is null ? "*" : $"{PolicyBuilder.GetBaseArn(methodArn)}/*/*")
                    .Build()
            );
        }

        // TODO :: Extract user information from token and set principalId
        var principalId = "user"; // Replace with actual user ID from token

        // Return an allow policy with optional context
        var response = new PolicyBuilder(principalId)
            .Allow(methodArn is null ? "*" : $"{PolicyBuilder.GetBaseArn(methodArn)}/*/*")
            .WithContext("userId", principalId)
            .WithContext("scope", "read:write")
            .Build();

        return Task.FromResult(response);
    })
#else
    .ForRequestAuthorizer()
    .HandleWith<ILoggerFactory>((handler, evt) =>
    {
        // TODO :: Replace ILoggerFactory with an interface that handles authorization logic
        var logger = handler.CreateLogger("RequestAuthorizer");

        // Access request parameters from the event
        var methodArn = evt.MethodArn;
        var headers = evt.Headers;
        var queryParams = evt.QueryStringParameters;
        var pathParams = evt.PathParameters;

        logger.LogInformation("Authorizing request for method: {MethodArn}", methodArn);

        // TODO :: Implement your authorization logic here
        // Example: Check headers for API key, validate request signature, etc.
        var authHeader = headers?.GetValueOrDefault("Authorization");
        var isValid = ValidateRequest(authHeader, queryParams, pathParams);

        if (!isValid)
        {
            // Return a deny policy
            return Task.FromResult(
                new PolicyBuilder("user")
                    .Deny(methodArn is null ? "*" : $"{PolicyBuilder.GetBaseArn(methodArn)}/*/*")
                    .Build()
            );
        }

        // TODO :: Extract user information and set principalId
        var principalId = "user"; // Replace with actual user ID

        // Return an allow policy with optional context
        var response = new PolicyBuilder(principalId)
            .Allow(methodArn is null ? "*" : $"{PolicyBuilder.GetBaseArn(methodArn)}/*/*")
            .WithContext("userId", principalId)
            .WithContext("ipAddress", evt.RequestContext?.Identity?.SourceIp ?? "unknown")
            .Build();

        return Task.FromResult(response);
    })
#endif
    .RunAsync();

// TODO :: Implement your validation logic
bool ValidateToken(string? token)
{
    // Replace with actual token validation logic
    return !string.IsNullOrEmpty(token);
}

bool ValidateRequest(string? authHeader, Dictionary<string, string>? queryParams, Dictionary<string, string>? pathParams)
{
    // Replace with actual request validation logic
    return !string.IsNullOrEmpty(authHeader);
}
