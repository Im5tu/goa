using Goa.Clients.Bedrock.Conversation.Chat;
using Goa.Clients.Bedrock.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Goa.Clients.Bedrock.Conversation;

/// <summary>
/// Extension methods for configuring Bedrock conversation services in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds the Bedrock chat session factory to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// This method registers <see cref="IChatSessionFactory"/> as a singleton.
    /// The factory depends on <see cref="IBedrockClient"/> and <see cref="IMcpToolAdapter"/>,
    /// which must be registered separately. An optional <see cref="IConversationStore"/>
    /// can be registered if conversation persistence is required.
    /// </remarks>
    public static IServiceCollection AddBedrockChatSession(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IChatSessionFactory>(sp =>
        {
            var client = sp.GetRequiredService<IBedrockClient>();
            var toolAdapter = sp.GetRequiredService<IMcpToolAdapter>();
            var store = sp.GetService<IConversationStore>();

            return new ChatSessionFactory(client, toolAdapter, store);
        });

        return services.AddBedrock();
    }
}
