using ErrorOr;
using Goa.Clients.Sns.Operations.Publish;

namespace Goa.Clients.Sns;

/// <summary>
/// High-performance SNS client interface optimized for AWS Lambda usage.
/// All operations use strongly-typed request objects and return ErrorOr results.
/// </summary>
public interface ISnsClient
{
    /// <summary>
    /// Publishes a message to an Amazon SNS topic, a text message (SMS message) directly to a phone number, or a message to a mobile platform endpoint.
    /// </summary>
    /// <param name="request">The publish request.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The publish response, or an error if the operation failed.</returns>
    Task<ErrorOr<PublishResponse>> PublishAsync(PublishRequest request, CancellationToken cancellationToken = default);
}