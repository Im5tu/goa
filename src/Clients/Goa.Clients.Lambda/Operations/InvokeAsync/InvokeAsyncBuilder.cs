namespace Goa.Clients.Lambda.Operations.InvokeAsync;

/// <summary>
/// Builder for creating Lambda asynchronous invoke requests with a fluent API.
/// </summary>
public sealed class InvokeAsyncBuilder
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
    public InvokeAsyncBuilder WithFunctionName(string functionName)
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
    public InvokeAsyncBuilder WithClientContext(string clientContext)
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
    public InvokeAsyncBuilder WithQualifier(string qualifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifier);
        _qualifier = qualifier;
        return this;
    }

    /// <summary>
    /// Sets the payload to send to the function.
    /// </summary>
    /// <param name="payload">The JSON payload as a string.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeAsyncBuilder WithPayload(string payload)
    {
        _payload = payload;
        return this;
    }

    /// <summary>
    /// Builds the asynchronous invoke request.
    /// </summary>
    /// <returns>A configured InvokeAsyncRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the function name is not set.</exception>
    public InvokeAsyncRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_functionName))
            throw new InvalidOperationException("Function name is required.");

        return new InvokeAsyncRequest
        {
            FunctionName = _functionName,
            ClientContext = _clientContext,
            Qualifier = _qualifier,
            Payload = _payload
        };
    }
}