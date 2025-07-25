namespace Goa.Clients.Lambda.Operations.InvokeDryRun;

/// <summary>
/// Builder for creating Lambda dry run invoke requests with a fluent API.
/// </summary>
public sealed class InvokeDryRunBuilder
{
    private string? _functionName;
    private string? _clientContext;
    private string? _qualifier;
    private string? _payload;

    /// <summary>
    /// Sets the function name or ARN.
    /// </summary>
    /// <param name="functionName">The function name or ARN.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeDryRunBuilder WithFunctionName(string functionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        _functionName = functionName;
        return this;
    }

    /// <summary>
    /// Sets the client context.
    /// </summary>
    /// <param name="clientContext">The client context (base64 encoded).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeDryRunBuilder WithClientContext(string clientContext)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientContext);
        _clientContext = clientContext;
        return this;
    }

    /// <summary>
    /// Sets the qualifier (version or alias).
    /// </summary>
    /// <param name="qualifier">The qualifier.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeDryRunBuilder WithQualifier(string qualifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifier);
        _qualifier = qualifier;
        return this;
    }

    /// <summary>
    /// Sets the payload to validate.
    /// </summary>
    /// <param name="payload">The JSON payload as a string.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeDryRunBuilder WithPayload(string payload)
    {
        _payload = payload;
        return this;
    }

    /// <summary>
    /// Builds the dry run invoke request.
    /// </summary>
    /// <returns>A configured InvokeDryRunRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the function name is not set.</exception>
    public InvokeDryRunRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_functionName))
            throw new InvalidOperationException("Function name is required.");

        return new InvokeDryRunRequest
        {
            FunctionName = _functionName,
            ClientContext = _clientContext,
            Qualifier = _qualifier,
            Payload = _payload
        };
    }
}