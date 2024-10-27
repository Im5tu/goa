namespace Goa.Functions.ApiGateway;

/// <summary>
///     Represents the context of the current Lambda Request
/// </summary>
public sealed class InvocationContext
{
    /// <summary>
    ///     Represents the request received from the Lambda Runtime API
    /// </summary>
    public required Request Request { get; init; }
    /// <summary>
    ///     Represents the response that will be returned to the Lambda Runtime API
    /// </summary>
    public required Response Response { get; init; }
}
