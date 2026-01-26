using ErrorOr;
using Goa.Clients.Bedrock.Operations.Converse;

namespace Goa.Clients.Bedrock;

/// <summary>
/// Extension methods for IBedrockClient providing builder pattern overloads.
/// </summary>
public static class BedrockExtensions
{
    /// <summary>
    /// Sends a conversation request to a Bedrock model using a builder pattern.
    /// </summary>
    /// <param name="client">The Bedrock client.</param>
    /// <param name="modelId">The identifier of the model to use.</param>
    /// <param name="configure">An action to configure the converse request using the builder.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The converse response, or an error if the operation failed.</returns>
    public static Task<ErrorOr<ConverseResponse>> ConverseAsync(
        this IBedrockClient client,
        string modelId,
        Action<ConverseBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(modelId);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new ConverseBuilder(modelId);
        configure(builder);
        return client.ConverseAsync(builder.Build(), cancellationToken);
    }
}
