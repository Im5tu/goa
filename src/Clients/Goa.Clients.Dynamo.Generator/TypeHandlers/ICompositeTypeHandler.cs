namespace Goa.Clients.Dynamo.Generator.TypeHandlers;

/// <summary>
/// Extended interface for type handlers that need to compose with other handlers.
/// Provides access to the registry to delegate nested type processing.
/// </summary>
public interface ICompositeTypeHandler : ITypeHandler
{
    /// <summary>
    /// Sets the registry reference for composition scenarios.
    /// Called by the registry after registration.
    /// </summary>
    void SetRegistry(TypeHandlerRegistry registry);
}