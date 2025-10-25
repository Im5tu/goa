namespace Goa.Functions.ApiGateway.Authorizer;

/// <summary>
/// Provides a fluent API for building IAM policy documents for API Gateway authorizers
/// </summary>
public class PolicyBuilder
{
    private readonly string _principalId;
    private readonly List<PolicyStatement> _statements = new();
    private Dictionary<string, object>? _context;
    private string? _usageIdentifierKey;

    /// <summary>
    /// Initializes a new instance of the PolicyBuilder class
    /// </summary>
    /// <param name="principalId">The principal identifier for the user</param>
    public PolicyBuilder(string principalId)
    {
        _principalId = principalId ?? throw new ArgumentNullException(nameof(principalId));
    }

    /// <summary>
    /// Allows access to the specified resource
    /// </summary>
    /// <param name="resource">The resource ARN or resource ARNs to allow</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder Allow(string resource)
    {
        _statements.Add(new PolicyStatement
        {
            Effect = Effect.Allow,
            Resource = resource
        });
        return this;
    }

    /// <summary>
    /// Allows access to multiple resources
    /// </summary>
    /// <param name="resources">The resource ARNs to allow</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder Allow(params string[] resources)
    {
        _statements.Add(new PolicyStatement
        {
            Effect = Effect.Allow,
            Resource = resources
        });
        return this;
    }

    /// <summary>
    /// Denies access to the specified resource
    /// </summary>
    /// <param name="resource">The resource ARN to deny</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder Deny(string resource)
    {
        _statements.Add(new PolicyStatement
        {
            Effect = Effect.Deny,
            Resource = resource
        });
        return this;
    }

    /// <summary>
    /// Denies access to multiple resources
    /// </summary>
    /// <param name="resources">The resource ARNs to deny</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder Deny(params string[] resources)
    {
        _statements.Add(new PolicyStatement
        {
            Effect = Effect.Deny,
            Resource = resources
        });
        return this;
    }

    /// <summary>
    /// Allows all resources matching the method ARN pattern
    /// </summary>
    /// <param name="methodArn">The base method ARN (will append /* for wildcard)</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder AllowAll(string methodArn)
    {
        var resource = methodArn.EndsWith("/*") ? methodArn : $"{methodArn}/*";
        return Allow(resource);
    }

    /// <summary>
    /// Denies all resources matching the method ARN pattern
    /// </summary>
    /// <param name="methodArn">The base method ARN (will append /* for wildcard)</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder DenyAll(string methodArn)
    {
        var resource = methodArn.EndsWith("/*") ? methodArn : $"{methodArn}/*";
        return Deny(resource);
    }

    /// <summary>
    /// Adds context data to be passed to the backend Lambda function
    /// </summary>
    /// <param name="key">The context key</param>
    /// <param name="value">The context value (must be string, number, or boolean)</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder WithContext(string key, object value)
    {
        _context ??= new Dictionary<string, object>();
        _context[key] = value;
        return this;
    }

    /// <summary>
    /// Sets the usage identifier key for API Gateway usage plans
    /// </summary>
    /// <param name="apiKey">The API key from the usage plan</param>
    /// <returns>The policy builder for method chaining</returns>
    public PolicyBuilder WithUsageIdentifierKey(string apiKey)
    {
        _usageIdentifierKey = apiKey;
        return this;
    }

    /// <summary>
    /// Builds the authorizer response
    /// </summary>
    /// <returns>The authorizer response with the configured policy</returns>
    public AuthorizerResponse Build()
    {
        if (_statements.Count == 0)
        {
            throw new InvalidOperationException("At least one policy statement (Allow or Deny) must be added before building the response.");
        }

        return new AuthorizerResponse
        {
            PrincipalId = _principalId,
            PolicyDocument = new PolicyDocument
            {
                Statement = _statements
            },
            Context = _context,
            UsageIdentifierKey = _usageIdentifierKey
        };
    }

    /// <summary>
    /// Builds a resource ARN from components
    /// </summary>
    /// <param name="region">AWS region</param>
    /// <param name="accountId">AWS account ID</param>
    /// <param name="apiId">API Gateway API ID</param>
    /// <param name="stage">API stage</param>
    /// <param name="verb">HTTP verb (or "*" for all)</param>
    /// <param name="resource">Resource path (or "*" for all)</param>
    /// <returns>The formatted resource ARN</returns>
    public static string BuildResourceArn(string region, string accountId, string apiId, string stage, string verb, string resource)
    {
        return $"arn:aws:execute-api:{region}:{accountId}:{apiId}/{stage}/{verb}/{resource}";
    }

    /// <summary>
    /// Extracts the base ARN from a method ARN by removing the method and resource portions
    /// </summary>
    /// <param name="methodArn">The full method ARN</param>
    /// <returns>The base ARN that can be used to build specific resource ARNs</returns>
    public static string GetBaseArn(string methodArn)
    {
        // Method ARN format: arn:aws:execute-api:region:accountId:apiId/stage/verb/resource
        // We want to extract: arn:aws:execute-api:region:accountId:apiId/stage
        var parts = methodArn.Split('/');
        if (parts.Length < 2)
        {
            return methodArn;
        }
        return string.Join("/", parts.Take(2));
    }
}
