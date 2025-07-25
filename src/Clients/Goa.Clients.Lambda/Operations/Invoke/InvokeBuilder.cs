using Goa.Clients.Lambda.Models;

namespace Goa.Clients.Lambda.Operations.Invoke;

/// <summary>
/// Builder for creating Lambda Invoke requests with a fluent API.
/// </summary>
public sealed class InvokeBuilder
{
    private string? _functionName;
    private InvocationType _invocationType = InvocationType.RequestResponse;
    private LogType _logType = LogType.None;
    private string? _clientContext;
    private string? _qualifier;
    private string? _payload;

    /// <summary>
    /// Sets the function name or ARN.
    /// </summary>
    /// <param name="functionName">The function name or ARN.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeBuilder WithFunctionName(string functionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(functionName);
        _functionName = functionName;
        return this;
    }

    /// <summary>
    /// Sets the invocation type.
    /// </summary>
    /// <param name="invocationType">The invocation type.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeBuilder WithInvocationType(InvocationType invocationType)
    {
        _invocationType = invocationType;
        return this;
    }

    /// <summary>
    /// Sets the log type.
    /// </summary>
    /// <param name="logType">The log type.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeBuilder WithLogType(LogType logType)
    {
        _logType = logType;
        return this;
    }

    /// <summary>
    /// Sets the client context.
    /// </summary>
    /// <param name="clientContext">The client context (base64 encoded).</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeBuilder WithClientContext(string clientContext)
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
    public InvokeBuilder WithQualifier(string qualifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifier);
        _qualifier = qualifier;
        return this;
    }

    /// <summary>
    /// Sets the payload to send to the function.
    /// </summary>
    /// <param name="payload">The payload object.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public InvokeBuilder WithPayload(string payload)
    {
        _payload = payload;
        return this;
    }

    /// <summary>
    /// Builds the Invoke request.
    /// </summary>
    /// <returns>A configured InvokeRequest.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the function name is not set.</exception>
    public InvokeRequest Build()
    {
        if (string.IsNullOrWhiteSpace(_functionName))
            throw new InvalidOperationException("Function name is required.");

        return new InvokeRequest
        {
            FunctionName = _functionName,
            InvocationType = _invocationType,
            LogType = _logType,
            ClientContext = _clientContext,
            Qualifier = _qualifier,
            Payload = _payload
        };
    }
}
