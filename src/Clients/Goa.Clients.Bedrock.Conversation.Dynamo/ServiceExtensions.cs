using Goa.Clients.Dynamo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Goa.Clients.Bedrock.Conversation.Dynamo;

/// <summary>
/// Extension methods for configuring the DynamoDB conversation store in dependency injection containers.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds the DynamoDB-backed conversation store to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="tableName">The optional table name for the conversation store.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBedrockDynamoConversationStore(
        this IServiceCollection services,
        string? tableName = null)
    {
        return services.AddBedrockDynamoConversationStore(config =>
        {
            if (tableName != null)
                config.TableName = tableName;
        });
    }

    /// <summary>
    /// Adds the DynamoDB-backed conversation store to the specified service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configure">An action to configure the store options.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBedrockDynamoConversationStore(
        this IServiceCollection services,
        Action<DynamoConversationStoreConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new DynamoConversationStoreConfiguration();
        configure(configuration);

        services.TryAddSingleton(configuration);
        services.TryAddSingleton<IConversationStore, DynamoConversationStore>();

        return services.AddBedrockChatSession().AddDynamoDB();
    }
}
