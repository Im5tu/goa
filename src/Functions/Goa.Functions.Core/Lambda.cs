namespace Goa.Functions.Core;

/// <summary>
///     Represents all lambda invocations
/// </summary>
/// <typeparam name="TRequest">The type of request that's being passed in</typeparam>
/// <typeparam name="TResponse">The type of response that's being returned</typeparam>
public interface ILambdaFunction<in TRequest, TResponse>
{
    /// <summary>
    ///     This method should not be called by anyone.
    /// </summary>
    Task<TResponse> InvokeAsync(TRequest request, CancellationToken cancellationToken);
}
