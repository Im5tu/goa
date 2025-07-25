using ErrorOr;
using Goa.Clients.EventBridge.Operations.PutEvents;

namespace Goa.Clients.EventBridge;

/// <summary>
/// High-performance EventBridge client interface optimized for AWS Lambda usage.
/// All operations use strongly-typed request objects and return ErrorOr results.
/// </summary>
public interface IEventBridgeClient
{
    /// <summary>
    /// Sends custom events to Amazon EventBridge so that they can be matched to rules.
    /// </summary>
    /// <param name="request">The put events request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The put events response, or an error if the operation failed.</returns>
    Task<ErrorOr<PutEventsResponse>> PutEventsAsync(PutEventsRequest request, CancellationToken cancellationToken = default);
}